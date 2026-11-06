import { test, expect } from '@playwright/test'

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

  await page.route('**/api/partidas', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue()
      return
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(partidas),
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
    await expect(page.getByText('Activa')).toBeVisible()
    await expect(page.getByText('Finalizada')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Finalizar' })).toBeVisible()
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

    await page.route('**/api/partidas', async (route) => {
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
        body: JSON.stringify(partidas),
      })
    })

    await page.goto('/')
    await page.getByRole('button', { name: /Bruma/ }).click()
    await page.getByRole('button', { name: 'Nueva partida' }).click()

    await expect(page.getByText('17')).toBeVisible()
    expect(createPayload).toEqual({ personajeId: character.id })
  })

  test('finaliza una partida activa y oculta la acción', async ({ page }) => {
    await prepareAuthenticatedPage(page, [activePartida])

    await page.route('**/api/partidas/17/finalizar', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(finalizedPartida),
      })
    })
    page.on('dialog', (dialog) => dialog.accept())

    await page.getByRole('button', { name: 'Finalizar' }).click()

    await expect(page.getByText('Finalizada')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Finalizar' })).toHaveCount(0)
  })

  test('muestra un error 401 sin romper la aplicación', async ({ page }) => {
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
    await page.route('**/api/partidas', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'La sesión no es válida.' }),
      })
    })

    await page.goto('/')
    await page.getByRole('button', { name: /Bruma/ }).click()

    await expect(page.getByRole('alert')).toContainText('La sesión no es válida.')
    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
  })

  test('muestra un error 404 al crear una partida sin romper la aplicación', async ({ page }) => {
    await prepareAuthenticatedPage(page)

    await page.route('**/api/partidas', async (route) => {
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
        body: JSON.stringify([]),
      })
    })

    await page.getByRole('button', { name: 'Nueva partida' }).click()

    await expect(page.getByRole('alert')).toContainText('El personaje no existe.')
    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
  })
})
