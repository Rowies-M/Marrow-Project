import { test, expect } from '@playwright/test'
// El listado siempre se pide con parÃ¡metros; este patrÃ³n tambiÃ©n cubre la creaciÃ³n sin parÃ¡metros.
const PARTIDAS_URL = /\/api\/partidas(\?.*)?$/

function pagina(items, extra = {}) {
  return { items, page: 1, pageSize: 10, totalItems: items.length, totalPages: items.length > 0 ? 1 : 0, ...extra }
}

const session = {
  token: 'mock-token-for-e2e',
  user: {
    id: 42,
    nombreUsuario: 'usuario-e2e',
    email: 'usuario-e2e@example.test',
  },
}

const character = {
  id: 7,
  nombre: 'Bruma',
  nivel: 1,
  nivelMaximo: 3,
  experiencia: 0,
  vidaActual: 100,
  vidaMaxima: 100,
  ataque: 10,
  defensa: 5,
  clase: 'Guerrero',
}

const activePartida = {
  id: 17,
  fechaCreacion: '2026-11-03T15:00:00Z',
  fechaUltimaActualizacion: '2026-11-03T15:00:00Z',
  estado: 'Activa',
  personajeId: character.id,
}

const finalizedPartida = {
  ...activePartida,
  fechaUltimaActualizacion: '2026-11-03T16:00:00Z',
  estado: 'Finalizada',
}

const listedFinalizedPartida = {
  ...finalizedPartida,
  id: 18,
}

async function prepareAuthenticatedPage(page, partidas = []) {
  await page.addInitScript((storedSession) => {
    sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
  }, session)

  await page.route('**/api/personajes', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([character]),
    })
  })

  await page.route(PARTIDAS_URL, async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue()
      return
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(pagina(partidas)),
    })
  })

  await page.goto('/')
  await page.getByRole('button', { name: /Bruma/ }).click()
  await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
}

test.describe('Partidas', () => {
  test('muestra las partidas y sus estados', async ({ page }) => {
    await prepareAuthenticatedPage(page, [activePartida, listedFinalizedPartida])

    await expect(page.getByText('17')).toBeVisible()
    await expect(page.getByRole('article').getByText('Activa')).toBeVisible()
    await expect(page.getByRole('article').getByText('Finalizada')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Finalizar' })).toBeVisible()
  })

  test('distingue victoria, derrota y abandono según el estado de cada partida', async ({ page }) => {
    const finalizadas = [
      { partida: { ...finalizedPartida, id: 21 }, resultadoCombate: 'Victoria' },
      { partida: { ...finalizedPartida, id: 22 }, resultadoCombate: 'Derrota' },
      { partida: { ...finalizedPartida, id: 23 }, resultadoCombate: null },
    ]
    for (const { partida, resultadoCombate } of finalizadas) {
      await page.route(`**/api/partidas/${partida.id}/estado`, async (route) => {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ estado: 'Finalizada', resultadoCombate }) })
      })
    }

    await prepareAuthenticatedPage(page, finalizadas.map(({ partida }) => partida))

    const cards = page.getByRole('article')
    await expect(cards.nth(0)).toContainText('Victoria')
    await expect(cards.nth(1)).toContainText('Derrota')
    await expect(cards.nth(2)).toContainText('Abandonada')
  })

  test('no consulta el resultado de una partida activa ni lo inventa si falla la lectura', async ({ page }) => {
    const consultadas = []
    await page.route('**/api/partidas/*/estado', async (route) => {
      consultadas.push(route.request().url())
      await route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({ detail: 'Error' }) })
    })

    await prepareAuthenticatedPage(page, [activePartida, listedFinalizedPartida])

    await expect.poll(() => consultadas.length).toBe(1)
    expect(consultadas[0]).toContain(`/api/partidas/${listedFinalizedPartida.id}/estado`)
    await expect(page.getByRole('article').getByText('Finalizada')).toBeVisible()
    await expect(page.getByText('Resultado')).toHaveCount(0)
    await expect(page.getByText('Abandonada')).toHaveCount(0)
    await expect(page.getByRole('alert')).toHaveCount(0)
  })

  test('muestra un mensaje cuando no existen partidas', async ({ page }) => {
    await prepareAuthenticatedPage(page)

    await expect(page.getByText(/Todav.*partidas/)).toBeVisible()
  })

  test('crea una partida con el personaje seleccionado', async ({ page }) => {
    let createPayload
    let partidas = []

    await page.addInitScript((storedSession) => {
      sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
    }, session)

    await page.route('**/api/personajes', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([character]),
      })
    })

    await page.route(PARTIDAS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        createPayload = route.request().postDataJSON()
        partidas = [activePartida]
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(activePartida),
        })
        return
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(pagina(partidas)),
      })
    })

    await page.goto('/')
    await page.getByRole('button', { name: /Bruma/ }).click()
    await page.getByRole('button', { name: 'Nueva partida' }).click()

    await expect(page.getByText('17')).toBeVisible()
    expect(createPayload).toEqual({ personajeId: character.id, dificultad: 'Normal' })
  })

  test('Difícil aparece bloqueada hasta que el personaje la desbloquea', async ({ page }) => {
    await prepareAuthenticatedPage(page)

    const selector = page.getByLabel('Dificultad de la nueva partida')
    await expect(selector).toHaveValue('Normal')
    await expect(selector.getByRole('option', { name: 'Difícil (bloqueada)' })).toBeDisabled()
    await expect(page.getByText('Ganá una partida en Normal para desbloquear Difícil.')).toBeVisible()
  })

  test('con Difícil desbloqueada, Normal sigue predeterminada y se puede elegir Difícil', async ({ page }) => {
    const unlockedCharacter = { ...character, dificultadDesbloqueada: 'Dificil' }
    const createPayloads = []

    await page.addInitScript((storedSession) => {
      sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
    }, session)
    await page.route('**/api/personajes', (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([unlockedCharacter]) }))
    await page.route(PARTIDAS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        createPayloads.push(route.request().postDataJSON())
        await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(activePartida) })
        return
      }

      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(pagina([])) })
    })

    await page.goto('/')
    await page.getByRole('button', { name: /Bruma/ }).click()

    const selector = page.getByLabel('Dificultad de la nueva partida')
    await expect(selector).toHaveValue('Normal')
    await expect(page.getByText('Ganá una partida en Normal para desbloquear Difícil.')).toHaveCount(0)

    await selector.selectOption('Dificil')
    await page.getByRole('button', { name: 'Nueva partida' }).click()

    await expect.poll(() => createPayloads).toEqual([{ personajeId: character.id, dificultad: 'Dificil' }])
  })

  test('finaliza una partida activa y oculta la acción', async ({ page }) => {
    const partidas = [activePartida]
    await prepareAuthenticatedPage(page, partidas)

    await page.route('**/api/partidas/17/finalizar', async (route) => {
      partidas[0] = finalizedPartida
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(finalizedPartida),
      })
    })
    page.on('dialog', (dialog) => dialog.accept())

    await page.getByRole('button', { name: 'Finalizar' }).click()

    await expect(page.getByRole('article').getByText('Finalizada')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Finalizar' })).toHaveCount(0)
  })

  test.describe('Filtros y paginación', () => {
    async function prepareQueryPage(page, respond) {
      const requests = []
      await page.addInitScript((storedSession) => {
        sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
      }, session)
      await page.route('**/api/personajes', async (route) => {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([character]) })
      })
      await page.route(PARTIDAS_URL, async (route) => {
        if (route.request().method() === 'POST') {
          await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(activePartida) })
          return
        }

        const params = new URL(route.request().url()).searchParams
        requests.push(Object.fromEntries(params))
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(respond(params)) })
      })

      await page.goto('/')
      await page.getByRole('button', { name: /Bruma/ }).click()
      await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
      return { requests, last: () => requests.at(-1) }
    }

    const threePages = (params) => pagina([activePartida], { page: Number(params.get('page')), totalItems: 25, totalPages: 3 })

    test('pide la primera página con los valores predeterminados', async ({ page }) => {
      const listing = await prepareQueryPage(page, threePages)

      await expect(page.getByText('Página 1 de 3 · 25 partidas')).toBeVisible()
      expect(listing.last()).toEqual({ orden: 'desc', page: '1', pageSize: '10' })
      await expect(page.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    })

    test('navega entre páginas y deshabilita el final', async ({ page }) => {
      const listing = await prepareQueryPage(page, threePages)

      await page.getByRole('button', { name: 'Siguiente' }).click()
      await expect(page.getByText('Página 2 de 3 · 25 partidas')).toBeVisible()
      expect(listing.last().page).toBe('2')

      await page.getByRole('button', { name: 'Siguiente' }).click()
      await expect(page.getByText('Página 3 de 3 · 25 partidas')).toBeVisible()
      await expect(page.getByRole('button', { name: 'Siguiente' })).toBeDisabled()

      await page.getByRole('button', { name: 'Anterior' }).click()
      await expect(page.getByText('Página 2 de 3 · 25 partidas')).toBeVisible()
    })

    test('combina los filtros y cada cambio vuelve a la primera página', async ({ page }) => {
      const listing = await prepareQueryPage(page, threePages)

      await page.getByRole('button', { name: 'Siguiente' }).click()
      await expect(page.getByText('Página 2 de 3')).toBeVisible()

      await page.getByLabel('Estado').selectOption('Activa')
      await expect.poll(() => listing.last()).toEqual({ orden: 'desc', page: '1', pageSize: '10', estado: 'Activa' })

      await page.locator('select[name="dificultad"]').selectOption('Dificil')
      await expect.poll(() => listing.last()).toEqual({ orden: 'desc', page: '1', pageSize: '10', estado: 'Activa', dificultad: 'Dificil' })

      await page.getByLabel('Orden').selectOption('asc')
      await expect.poll(() => listing.last()).toEqual({ orden: 'asc', page: '1', pageSize: '10', estado: 'Activa', dificultad: 'Dificil' })

      await page.getByLabel('Estado').selectOption('')
      await expect.poll(() => listing.last()).toEqual({ orden: 'asc', page: '1', pageSize: '10', dificultad: 'Dificil' })
    })

    test('conserva los filtros y la página al volver de una partida', async ({ page }) => {
      await page.route('**/api/partidas/17/estado', async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ partidaId: 17, estado: 'Activa', personajeNombre: 'Bruma', enemigoNombre: 'Goblin', fechaCreacion: activePartida.fechaCreacion, personajeExperiencia: 0, personajeExperienciaSiguienteNivel: 100 }),
        })
      })
      const listing = await prepareQueryPage(page, threePages)
      await page.getByLabel('Estado').selectOption('Activa')
      await page.getByRole('button', { name: 'Siguiente' }).click()
      await expect(page.getByText('Página 2 de 3')).toBeVisible()

      await page.getByRole('button', { name: 'Jugar' }).click()
      await page.getByRole('button', { name: 'Volver a partidas' }).click()

      await expect(page.getByLabel('Estado')).toHaveValue('Activa')
      await expect(page.getByText('Página 2 de 3')).toBeVisible()
      expect(listing.last()).toEqual({ orden: 'desc', page: '2', pageSize: '10', estado: 'Activa' })
    })

    test('si la página quedó vacía vuelve a la última con resultados', async ({ page }) => {
      const partidas = [activePartida]
      const listing = await prepareQueryPage(page, (params) => {
        const requested = Number(params.get('page'))
        return requested === 1
          ? pagina(partidas, { totalItems: 11, totalPages: 2 })
          : pagina(partidas.length > 0 ? partidas : [], { page: requested, totalItems: 10, totalPages: 1 })
      })
      await page.getByRole('button', { name: 'Siguiente' }).click()
      await expect(page.getByText(/Página 2/)).toBeVisible()

      await page.route('**/api/partidas/17/finalizar', async (route) => {
        partidas.length = 0
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(finalizedPartida) })
      })
      page.once('dialog', (dialog) => dialog.accept())
      await page.getByRole('button', { name: 'Finalizar' }).click()

      // Solo importan las últimas: en desarrollo, StrictMode duplica la primera carga.
      await expect.poll(() => listing.requests.map((request) => request.page).slice(-3)).toEqual(['2', '2', '1'])
    })

    test('un error de la API no se confunde con una lista vacía y se puede reintentar', async ({ page }) => {
      let shouldFail = true
      await page.addInitScript((storedSession) => {
        sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
      }, session)
      await page.route('**/api/personajes', async (route) => {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([character]) })
      })
      await page.route(PARTIDAS_URL, async (route) => {
        if (shouldFail) {
          await route.fulfill({ status: 500, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Ocurrió un error inesperado.', status: 500 }) })
          return
        }

        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(pagina([activePartida])) })
      })

      await page.goto('/')
      await page.getByRole('button', { name: /Bruma/ }).click()

      await expect(page.getByRole('alert')).toContainText('Ocurrió un error inesperado.')
      await expect(page.getByText(/Todav.*partidas/)).toHaveCount(0)

      shouldFail = false
      await page.getByRole('button', { name: 'Reintentar' }).click()

      await expect(page.getByRole('button', { name: 'Jugar' })).toBeVisible()
      await expect(page.getByRole('alert')).toHaveCount(0)
    })

    test('un corte de red muestra un mensaje comprensible', async ({ page }) => {
      await page.addInitScript((storedSession) => {
        sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
      }, session)
      await page.route('**/api/personajes', async (route) => {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([character]) })
      })
      await page.route(PARTIDAS_URL, (route) => route.abort())

      await page.goto('/')
      await page.getByRole('button', { name: /Bruma/ }).click()

      await expect(page.getByRole('alert')).toContainText('No se pudo conectar con el servidor.')
      await expect(page.getByRole('button', { name: 'Reintentar' })).toBeVisible()
    })

    test('avisa cuando los filtros no tienen resultados', async ({ page }) => {
      await prepareQueryPage(page, () => pagina([]))

      await expect(page.getByText(/Todav.*partidas/)).toBeVisible()
      await page.getByLabel('Estado').selectOption('Finalizada')

      await expect(page.getByText('No hay partidas con estos filtros.')).toBeVisible()
      await expect(page.getByRole('navigation', { name: 'Paginación de partidas' })).toHaveCount(0)
    })

    test('al crear una partida limpia los filtros para mostrarla', async ({ page }) => {
      const listing = await prepareQueryPage(page, threePages)
      await page.getByLabel('Estado').selectOption('Finalizada')
      await expect.poll(() => listing.last().estado).toBe('Finalizada')

      await page.getByRole('button', { name: 'Nueva partida' }).click()

      await expect.poll(() => listing.last()).toEqual({ orden: 'desc', page: '1', pageSize: '10' })
      await expect(page.getByLabel('Estado')).toHaveValue('')
    })
  })

  test('con la sesión vencida vuelve al login y lo informa', async ({ page }) => {
    await page.addInitScript((storedSession) => {
      sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
    }, session)

    await page.route('**/api/personajes', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([character]),
      })
    })
    await page.route(PARTIDAS_URL, async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'La sesión no es válida.' }),
      })
    })

    await page.goto('/')
    await page.getByRole('button', { name: /Bruma/ }).click()

    await expect(page.getByText('Tu sesión expiró. Volvé a iniciar sesión.')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Iniciar sesión' })).toBeVisible()
    expect(await page.evaluate(() => sessionStorage.getItem('marrow.auth.session'))).toBeNull()
  })

  test('muestra un error 404 al crear una partida sin romper la aplicación', async ({ page }) => {
    await prepareAuthenticatedPage(page)

    await page.route(PARTIDAS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 404,
          contentType: 'application/json',
          body: JSON.stringify({ detail: 'El personaje no existe.' }),
        })
        return
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(pagina([])),
      })
    })

    await page.getByRole('button', { name: 'Nueva partida' }).click()

    await expect(page.getByRole('alert')).toContainText('El personaje no existe.')
    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
  })
})
