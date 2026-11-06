import { test, expect } from '@playwright/test'

const session = {
  token: 'mock-token-for-game-e2e',
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

const finalizedPartida = { ...activePartida, id: 18, estado: 'Finalizada' }

const gameState = {
  partidaId: activePartida.id,
  fechaCreacion: activePartida.fechaCreacion,
  fechaUltimaActualizacion: activePartida.fechaUltimaActualizacion,
  estado: activePartida.estado,
  personajeId: character.id,
  personajeNombre: character.nombre,
  personajeNivel: character.nivel,
  personajeExperiencia: 30,
  personajeExperienciaSiguienteNivel: 100,
  vidaPersonajeActual: 84,
  vidaPersonajeMaxima: character.vidaMaxima,
  personajeX: 1,
  personajeY: 1,
  enemigoId: 1,
  enemigoNombre: 'Goblin',
  enemigoNivel: 1,
  vidaEnemigoActual: 30,
  vidaEnemigoMaxima: 30,
  enemigoX: 5,
  enemigoY: 3,
  dificultad: 'Dificil',
  resultadoCombate: null,
}

async function preparePage(page, partidas) {
  await page.addInitScript((storedSession) => {
    sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
  }, session)

  await page.route('**/api/personajes', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([character]) })
  })

  await page.route('**/api/partidas', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(partidas) })
  })

  await page.route(`**/api/partidas/${activePartida.id}/estado`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(gameState) })
  })

  await page.goto('/')
  await page.getByRole('button', { name: /Bruma/ }).click()
  await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
}

async function prepareMovablePage(page, initialState = gameState, movementResponse = null) {
  await page.addInitScript((storedSession) => {
    sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
  }, session)

  let currentState = { ...initialState }
  const movementRequests = []

  await page.route('**/api/personajes', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([character]) })
  })
  await page.route('**/api/partidas', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([activePartida]) })
  })
  await page.route(`**/api/partidas/${activePartida.id}/estado`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(currentState) })
  })
  await page.route(`**/api/partidas/${activePartida.id}/movimiento`, async (route) => {
    const requestBody = JSON.parse(route.request().postData() || '{}')
    movementRequests.push(requestBody.direccion)

    if (movementResponse) {
      await route.fulfill({ status: movementResponse.status, contentType: 'application/json', body: JSON.stringify(movementResponse.body) })
      return
    }

    const offsets = { arriba: [0, -1], abajo: [0, 1], izquierda: [-1, 0], derecha: [1, 0] }
    const [offsetX, offsetY] = offsets[requestBody.direccion]
    currentState = {
      ...currentState,
      personajeX: Math.max(0, Math.min(10, currentState.personajeX + offsetX)),
      personajeY: Math.max(0, Math.min(6, currentState.personajeY + offsetY)),
    }
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(currentState) })
  })

  await page.goto('/')
  await page.getByRole('button', { name: /Bruma/ }).click()
  await page.getByRole('button', { name: 'Jugar' }).click()
  await expect(page.getByRole('heading', { name: 'La entrada a la mazmorra' })).toBeVisible()

  return { movementRequests, getCurrentState: () => currentState }
}

test.describe('Pantalla de juego', () => {
  test('accede desde una partida activa y muestra la escena visual', async ({ page }) => {
    await preparePage(page, [activePartida])

    await page.getByRole('button', { name: 'Jugar' }).click()

    await expect(page.getByRole('heading', { name: 'La entrada a la mazmorra' })).toBeVisible()
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toBeVisible()
    await expect(page.getByLabel('Goblin', { exact: true })).toBeVisible()
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Vida: 84/100')
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Vida: 30/30')
  })

  test('regresa al panel de partidas', async ({ page }) => {
    await preparePage(page, [activePartida])

    await page.getByRole('button', { name: 'Jugar' }).click()
    await page.getByRole('button', { name: 'Volver a partidas' }).click()

    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Jugar' })).toBeVisible()
  })

  test('no muestra la acción de jugar para partidas finalizadas', async ({ page }) => {
    await preparePage(page, [finalizedPartida])

    await expect(page.getByText('Finalizada')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Jugar' })).toHaveCount(0)
  })

  test('muestra un error y no conserva datos si falla la carga del estado', async ({ page }) => {
    await page.addInitScript((storedSession) => {
      sessionStorage.setItem('marrow.auth.session', JSON.stringify(storedSession))
    }, session)

    await page.route('**/api/personajes', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([character]) })
    })
    await page.route('**/api/partidas', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([activePartida]) })
    })
    await page.route(`**/api/partidas/${activePartida.id}/estado`, async (route) => {
      await route.fulfill({ status: 404, contentType: 'application/json', body: JSON.stringify({ detail: 'La partida no existe.' }) })
    })

    await page.goto('/')
    await page.getByRole('button', { name: /Bruma/ }).click()
    await page.getByRole('button', { name: 'Jugar' }).click()

    await expect(page.getByRole('alert')).toContainText('La partida no existe.')
    await expect(page.getByLabel('Escenario de juego')).toHaveCount(0)
    await expect(page.getByText('Vida: 84/100')).toHaveCount(0)
  })

  test('mueve al guerrero en las cuatro direcciones', async ({ page }) => {
    const movement = await prepareMovablePage(page)
    const warrior = page.getByLabel('Guerrero Bruma', { exact: true })

    await page.keyboard.press('ArrowUp')
    await expect.poll(() => movement.movementRequests.length).toBe(1)
    await expect(warrior).toHaveAttribute('style', /top: 0%;/)
    await page.keyboard.press('ArrowLeft')
    await expect.poll(() => movement.movementRequests.length).toBe(2)
    await expect(warrior).toHaveAttribute('style', /left: 0%;/)
    await page.keyboard.press('s')
    await expect.poll(() => movement.movementRequests.length).toBe(3)
    await expect(warrior).toHaveAttribute('style', /top: 16\.6/)
    await page.keyboard.press('d')

    await expect.poll(() => movement.movementRequests).toEqual(['arriba', 'izquierda', 'abajo', 'derecha'])
    await expect.poll(() => movement.getCurrentState().personajeX).toBe(1)
    await expect.poll(() => movement.getCurrentState().personajeY).toBe(1)
    await expect(warrior).toHaveAttribute('style', /left: 10%; top: 16.6667%;/)
  })

  test('impide salir de los límites del escenario', async ({ page }) => {
    const movement = await prepareMovablePage(page, { ...gameState, personajeX: 0, personajeY: 0 })

    await page.keyboard.press('ArrowLeft')
    await expect.poll(() => movement.movementRequests.length).toBe(1)
    await page.keyboard.press('ArrowUp')

    await expect.poll(() => movement.movementRequests).toEqual(['izquierda', 'arriba'])
    await expect.poll(() => movement.getCurrentState().personajeX).toBe(0)
    await expect.poll(() => movement.getCurrentState().personajeY).toBe(0)
  })

  test('persiste la posición y la recupera al recargar', async ({ page }) => {
    const movement = await prepareMovablePage(page)

    await page.keyboard.press('ArrowRight')
    await expect.poll(() => movement.getCurrentState().personajeX).toBe(2)
    await page.reload()

    await page.getByRole('button', { name: 'Jugar' }).click()
    await expect(page.getByRole('heading', { name: 'La entrada a la mazmorra' })).toBeVisible()
    await expect.poll(() => movement.getCurrentState().personajeX).toBe(2)
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toHaveAttribute('style', /left: 20%; top: 16\.6/)
  })

  test('muestra el error de movimiento y conserva la última posición confirmada', async ({ page }) => {
    await prepareMovablePage(page, gameState, {
      status: 409,
      body: { detail: 'La partida no está activa.' },
    })

    await page.keyboard.press('ArrowRight')

    await expect(page.getByRole('alert')).toContainText('La partida no está activa.')
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toHaveAttribute('style', /left: 10%; top: 16\.6/)
  })

  test('muestra la información de la partida y del enemigo', async ({ page }) => {
    await prepareMovablePage(page)

    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Goblin')
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Nivel 1')
    await expect(page.getByRole('region', { name: 'Información de la partida' })).toContainText('#17')
    await expect(page.getByRole('region', { name: 'Información de la partida' })).toContainText('Difícil')
    await expect(page.getByRole('region', { name: 'Información de la partida' })).toContainText('Activa')
  })

  test('acepta W, A, S y D también en mayúsculas', async ({ page }) => {
    const movement = await prepareMovablePage(page)

    await page.keyboard.press('W')
    await expect.poll(() => movement.movementRequests).toEqual(['arriba'])
    await page.keyboard.press('D')
    await expect.poll(() => movement.movementRequests).toEqual(['arriba', 'derecha'])
  })

  test('no repite el movimiento al mantener la tecla ni responde a atajos', async ({ page }) => {
    const movement = await prepareMovablePage(page)
    const warrior = page.getByLabel('Guerrero Bruma', { exact: true })

    await page.keyboard.down('ArrowRight')
    await expect(warrior).toHaveAttribute('style', /left: 20%;/)
    await page.keyboard.down('ArrowRight')
    await page.keyboard.down('ArrowRight')
    await page.keyboard.up('ArrowRight')
    await page.keyboard.press('Control+s')

    await expect(page.getByText('Guardando movimiento...')).toHaveCount(0)
    expect(movement.movementRequests).toEqual(['derecha'])
  })

  test('no atraviesa la casilla ocupada por el enemigo', async ({ page }) => {
    await prepareMovablePage(page, { ...gameState, personajeX: 4, personajeY: 3 }, {
      status: 409,
      body: { detail: 'La casilla está ocupada por el enemigo.' },
    })

    await page.keyboard.press('ArrowRight')

    await expect(page.getByRole('alert')).toContainText('La casilla está ocupada por el enemigo.')
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toHaveAttribute('style', /left: 40%; top: 50%;/)
  })

  test('abandona la partida al confirmar y vuelve al panel', async ({ page }) => {
    await prepareMovablePage(page)
    let abandonRequests = 0
    await page.route(`**/api/partidas/${activePartida.id}/finalizar`, async (route) => {
      abandonRequests += 1
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ...activePartida, estado: 'Finalizada' }) })
    })
    page.once('dialog', (dialog) => dialog.accept())

    await page.getByRole('button', { name: 'Abandonar partida' }).click()

    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
    expect(abandonRequests).toBe(1)
  })

  test('no abandona si se cancela y muestra el error si la API lo rechaza', async ({ page }) => {
    await prepareMovablePage(page)
    let abandonRequests = 0
    await page.route(`**/api/partidas/${activePartida.id}/finalizar`, async (route) => {
      abandonRequests += 1
      await route.fulfill({ status: 409, contentType: 'application/json', body: JSON.stringify({ detail: 'La partida no está activa.' }) })
    })
    const abandon = page.getByRole('button', { name: 'Abandonar partida' })

    page.once('dialog', (dialog) => dialog.dismiss())
    await abandon.click()
    expect(abandonRequests).toBe(0)

    page.once('dialog', (dialog) => dialog.accept())
    await abandon.click()

    await expect(page.getByRole('alert')).toContainText('La partida no está activa.')
    await expect(page.getByRole('heading', { name: 'La entrada a la mazmorra' })).toBeVisible()
    await expect(abandon).toBeEnabled()
  })
})

test.describe('Combate cuerpo a cuerpo', () => {
  const adjacentState = { ...gameState, personajeX: 4, personajeY: 3, vidaPersonajeActual: 80, vidaPersonajeMaxima: 80, vidaEnemigoActual: 40, vidaEnemigoMaxima: 40 }

  function attackResult(changes, danioInfligido, danioRecibido) {
    return { estado: { ...adjacentState, ...changes }, danioInfligido, danioRecibido }
  }

  async function mockAttack(page, respond) {
    const requests = []
    await page.route(`**/api/partidas/${activePartida.id}/ataque`, async (route) => {
      requests.push({ method: route.request().method(), body: route.request().postData() })
      await respond(route)
    })
    return requests
  }

  const fulfillJson = (route, status, body) => route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) })

  test('ataque válido: muestra el daño real de la API y permite continuar', async ({ page }) => {
    await prepareMovablePage(page, adjacentState)
    const requests = await mockAttack(page, (route) => fulfillJson(route, 200, attackResult({ vidaPersonajeActual: 70, vidaEnemigoActual: 25 }, 15, 10)))
    const hud = page.getByRole('region', { name: 'HUD de la partida' })

    await page.getByRole('button', { name: 'Atacar' }).click()

    await expect(hud).toContainText('Vida: 70/80')
    await expect(hud).toContainText('Vida: 25/40')
    await expect(page.getByText('Golpeaste a Goblin por 15 de daño.')).toBeVisible()
    await expect(page.getByText('Goblin te golpeó por 10 de daño.')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Atacar' })).toBeEnabled()
    await expect(page.getByRole('group', { name: 'Resultado de la partida' })).toHaveCount(0)
    expect(requests).toEqual([{ method: 'POST', body: null }])
  })

  test('la barra espaciadora ataca una sola vez aunque se mantenga', async ({ page }) => {
    await prepareMovablePage(page, adjacentState)
    const requests = await mockAttack(page, (route) => fulfillJson(route, 200, attackResult({ vidaPersonajeActual: 70, vidaEnemigoActual: 25 }, 15, 10)))

    await page.keyboard.down('Space')
    await expect(page.getByText('Golpeaste a Goblin por 15 de daño.')).toBeVisible()
    await page.keyboard.down('Space')
    await page.keyboard.up('Space')

    await expect(page.getByRole('button', { name: 'Atacar' })).toBeEnabled()
    expect(requests).toHaveLength(1)
  })

  test('deshabilita el ataque mientras hay una petición en curso', async ({ page }) => {
    await prepareMovablePage(page, adjacentState)
    let release
    const pending = new Promise((resolve) => { release = resolve })
    const requests = await mockAttack(page, async (route) => {
      await pending
      await fulfillJson(route, 200, attackResult({ vidaPersonajeActual: 70, vidaEnemigoActual: 25 }, 15, 10))
    })

    await page.keyboard.press('Space')
    const attacking = page.getByRole('button', { name: 'Atacando...' })
    await expect(attacking).toBeDisabled()
    await page.keyboard.press('Space')
    await expect(page.getByRole('button', { name: 'Abandonar partida' })).toBeDisabled()
    release()

    await expect(page.getByRole('button', { name: 'Atacar' })).toBeEnabled()
    expect(requests).toHaveLength(1)
  })

  test('ataque inválido: lejos del enemigo no envía la petición', async ({ page }) => {
    await prepareMovablePage(page)
    const requests = await mockAttack(page, (route) => fulfillJson(route, 200, attackResult({}, 15, 10)))

    await expect(page.getByRole('button', { name: 'Atacar' })).toBeDisabled()
    await expect(page.getByText('Acercate a Goblin para atacar.')).toBeVisible()
    await page.keyboard.press('Space')
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toBeVisible()

    expect(requests).toHaveLength(0)
  })

  test('tampoco considera adyacente una casilla en diagonal', async ({ page }) => {
    await prepareMovablePage(page, { ...adjacentState, personajeX: 4, personajeY: 2 })

    await expect(page.getByRole('button', { name: 'Atacar' })).toBeDisabled()
  })

  test('ataque inválido: muestra el rechazo de la API y vuelve a leer el estado', async ({ page }) => {
    await prepareMovablePage(page, adjacentState)
    await mockAttack(page, (route) => fulfillJson(route, 409, { title: 'Ataque no permitido.', detail: 'El enemigo está fuera de alcance.' }))
    await page.route(`**/api/partidas/${activePartida.id}/estado`, (route) => fulfillJson(route, 200, { ...adjacentState, personajeX: 1, personajeY: 1 }))

    await page.getByRole('button', { name: 'Atacar' }).click()

    await expect(page.getByRole('alert')).toContainText('El enemigo está fuera de alcance.')
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Vida: 40/40')
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toHaveAttribute('style', /left: 10%; top: 16\.6/)
    await expect(page.getByRole('button', { name: 'Atacar' })).toBeDisabled()
  })

  test('victoria: el enemigo no contraataca y se bloquean las acciones', async ({ page }) => {
    const movement = await prepareMovablePage(page, { ...adjacentState, vidaEnemigoActual: 10 })
    const requests = await mockAttack(page, (route) => fulfillJson(route, 200, attackResult({ estado: 'Finalizada', resultadoCombate: 'Victoria', vidaEnemigoActual: 0 }, 10, 0)))

    await page.getByRole('button', { name: 'Atacar' }).click()

    const result = page.getByRole('group', { name: 'Resultado de la partida' })
    await expect(result.getByRole('heading', { name: 'Victoria' })).toBeVisible()
    await expect(page.getByText('Golpeaste a Goblin por 10 de daño.')).toBeVisible()
    await expect(page.getByText(/te golpeó/)).toHaveCount(0)
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Vida: 0/40')
    await expect(page.getByRole('button', { name: 'Atacar' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Abandonar partida' })).toHaveCount(0)

    await page.keyboard.press('ArrowLeft')
    await page.keyboard.press('Space')
    expect(movement.movementRequests).toHaveLength(0)
    expect(requests).toHaveLength(1)

    await result.getByRole('button', { name: 'Volver al listado de partidas' }).click()
    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
  })

  test('derrota: muestra el contraataque final y bloquea las acciones', async ({ page }) => {
    const movement = await prepareMovablePage(page, { ...adjacentState, vidaPersonajeActual: 10 })
    await mockAttack(page, (route) => fulfillJson(route, 200, attackResult({ estado: 'Finalizada', resultadoCombate: 'Derrota', vidaPersonajeActual: 0, vidaEnemigoActual: 25 }, 15, 10)))

    await page.keyboard.press('Space')

    const result = page.getByRole('group', { name: 'Resultado de la partida' })
    await expect(result.getByRole('heading', { name: 'Derrota' })).toBeVisible()
    await expect(page.getByText('Goblin te golpeó por 10 de daño.')).toBeVisible()
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Vida: 0/80')
    await expect(page.getByRole('button', { name: 'Atacar' })).toHaveCount(0)

    await page.keyboard.press('ArrowLeft')
    expect(movement.movementRequests).toHaveLength(0)
  })

  test('muestra el progreso de experiencia del personaje', async ({ page }) => {
    await prepareMovablePage(page, adjacentState)

    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Nivel 1 · XP 30/100')
    await expect(page.getByRole('progressbar', { name: 'Experiencia de Guerrero Bruma' })).toHaveAttribute('aria-valuenow', '30')
  })

  test('una victoria muestra la experiencia y el nivel que informa la API', async ({ page }) => {
    await prepareMovablePage(page, { ...adjacentState, vidaEnemigoActual: 10 })
    await mockAttack(page, (route) => fulfillJson(route, 200, {
      ...attackResult({ estado: 'Finalizada', resultadoCombate: 'Victoria', vidaEnemigoActual: 0, personajeNivel: 2, personajeExperiencia: 30, personajeExperienciaSiguienteNivel: 200 }, 10, 0),
      experienciaGanada: 100,
    }))

    await page.getByRole('button', { name: 'Atacar' }).click()

    const result = page.getByRole('group', { name: 'Resultado de la partida' })
    await expect(result).toContainText('+100 de experiencia · ¡Nivel 2!')
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Nivel 2 · XP 30/200')
  })

  test('una victoria sin subir de nivel no anuncia un nivel nuevo', async ({ page }) => {
    await prepareMovablePage(page, { ...adjacentState, vidaEnemigoActual: 10, personajeNivel: 3, personajeExperiencia: 0, personajeExperienciaSiguienteNivel: 300 })
    await mockAttack(page, (route) => fulfillJson(route, 200, {
      ...attackResult({ estado: 'Finalizada', resultadoCombate: 'Victoria', vidaEnemigoActual: 0, personajeNivel: 3, personajeExperiencia: 100, personajeExperienciaSiguienteNivel: 300 }, 10, 0),
      experienciaGanada: 100,
    }))

    await page.getByRole('button', { name: 'Atacar' }).click()

    const result = page.getByRole('group', { name: 'Resultado de la partida' })
    await expect(result).toContainText('+100 de experiencia')
    await expect(result).not.toContainText('¡Nivel')
  })

  test('una derrota no muestra experiencia ganada', async ({ page }) => {
    await prepareMovablePage(page, { ...adjacentState, vidaPersonajeActual: 10 })
    await mockAttack(page, (route) => fulfillJson(route, 200, {
      ...attackResult({ estado: 'Finalizada', resultadoCombate: 'Derrota', vidaPersonajeActual: 0 }, 15, 10),
      experienciaGanada: 0,
    }))

    await page.getByRole('button', { name: 'Atacar' }).click()

    const result = page.getByRole('group', { name: 'Resultado de la partida' })
    await expect(result.getByRole('heading', { name: 'Derrota' })).toBeVisible()
    await expect(result).not.toContainText('experiencia')
    await expect(page.getByRole('region', { name: 'HUD de la partida' })).toContainText('Nivel 1 · XP 30/100')
  })

  test('al volver al listado muestra el nivel actualizado del personaje', async ({ page }) => {
    await prepareMovablePage(page, { ...adjacentState, vidaEnemigoActual: 10 })
    await mockAttack(page, (route) => fulfillJson(route, 200, {
      ...attackResult({ estado: 'Finalizada', resultadoCombate: 'Victoria', vidaEnemigoActual: 0, personajeNivel: 2 }, 10, 0),
      experienciaGanada: 100,
    }))
    await page.route(`**/api/personajes/${character.id}`, (route) => fulfillJson(route, 200, { ...character, nivel: 2, experiencia: 30 }))

    await page.getByRole('button', { name: 'Atacar' }).click()
    await page.getByRole('button', { name: 'Volver al listado de partidas' }).click()

    await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
    await expect(page.getByText('Nivel', { exact: true }).locator('..')).toContainText('2')
  })

  test('una partida finalizada sin resultado no otorga victoria ni permite reanudarla', async ({ page }) => {
    await prepareMovablePage(page, { ...adjacentState, estado: 'Finalizada', resultadoCombate: null })

    const result = page.getByRole('group', { name: 'Resultado de la partida' })
    await expect(result.getByRole('heading', { name: 'Partida finalizada' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Victoria' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Atacar' })).toHaveCount(0)
    await expect(page.getByText('La aventura de Bruma terminó.')).toBeVisible()
    await expect(page.getByText('Bruma se prepara para avanzar.')).toHaveCount(0)
  })

  test('sin pantalla táctil no muestra los controles en pantalla', async ({ page }) => {
    await prepareMovablePage(page)

    await expect(page.getByRole('region', { name: 'Controles táctiles' })).toHaveCount(0)
    await expect(page.getByText('Bruma se prepara para avanzar.')).toBeVisible()
  })
})

test.describe('Controles táctiles', () => {
  test.use({ hasTouch: true, isMobile: true, viewport: { width: 375, height: 812 } })

  const adjacentState = { ...gameState, personajeX: 4, personajeY: 3 }
  const moveButton = (page, name) => page.getByRole('region', { name: 'Controles táctiles' }).getByRole('button', { name })
  const allMoveButtons = (page) => page.getByRole('region', { name: 'Controles táctiles' }).getByRole('button')

  test('mueve en las cuatro direcciones con el mismo endpoint que el teclado', async ({ page }) => {
    const movement = await prepareMovablePage(page)
    const warrior = page.getByLabel('Guerrero Bruma', { exact: true })

    await moveButton(page, 'Mover arriba').tap()
    await expect(warrior).toHaveAttribute('style', /top: 0%;/)
    await moveButton(page, 'Mover a la izquierda').tap()
    await expect(warrior).toHaveAttribute('style', /left: 0%;/)
    await moveButton(page, 'Mover abajo').tap()
    await expect(warrior).toHaveAttribute('style', /top: 16\.6/)
    await moveButton(page, 'Mover a la derecha').tap()
    await expect(warrior).toHaveAttribute('style', /left: 10%;/)

    expect(movement.movementRequests).toEqual(['arriba', 'izquierda', 'abajo', 'derecha'])
  })

  test('respeta el límite del mapa y el bloqueo del backend', async ({ page }) => {
    const movement = await prepareMovablePage(page, { ...gameState, personajeX: 0, personajeY: 0 })

    await moveButton(page, 'Mover a la izquierda').tap()
    await expect.poll(() => movement.movementRequests).toEqual(['izquierda'])
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toHaveAttribute('style', /left: 0%; top: 0%;/)
  })

  test('muestra el rechazo por la casilla del enemigo junto a los controles', async ({ page }) => {
    await prepareMovablePage(page, adjacentState, { status: 409, body: { detail: 'La casilla está ocupada por el enemigo.' } })

    await moveButton(page, 'Mover a la derecha').tap()

    await expect(page.getByRole('alert')).toContainText('La casilla está ocupada por el enemigo.')
    await expect(page.getByLabel('Guerrero Bruma', { exact: true })).toHaveAttribute('style', /left: 40%; top: 50%;/)
  })

  test('deshabilita los controles durante una petición', async ({ page }) => {
    await prepareMovablePage(page)
    let release
    const pending = new Promise((resolve) => { release = resolve })
    let requests = 0
    await page.route(`**/api/partidas/${activePartida.id}/movimiento`, async (route) => {
      requests += 1
      await pending
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ...gameState, personajeX: 2 }) })
    })

    await moveButton(page, 'Mover a la derecha').tap()

    for (const button of await allMoveButtons(page).all()) {
      await expect(button).toBeDisabled()
    }
    release()
    await expect(moveButton(page, 'Mover a la derecha')).toBeEnabled()
    expect(requests).toBe(1)
  })

  test('no permite mover al personaje después de finalizar', async ({ page }) => {
    const movement = await prepareMovablePage(page, { ...adjacentState, vidaEnemigoActual: 10 })
    await page.route(`**/api/partidas/${activePartida.id}/ataque`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ estado: { ...adjacentState, estado: 'Finalizada', resultadoCombate: 'Victoria', vidaEnemigoActual: 0 }, danioInfligido: 10, danioRecibido: 0 }),
      })
    })

    await page.getByRole('button', { name: 'Atacar' }).tap()

    await expect(page.getByRole('heading', { name: 'Victoria' })).toBeVisible()
    await expect(allMoveButtons(page)).toHaveCount(4)
    for (const button of await allMoveButtons(page).all()) {
      await expect(button).toBeDisabled()
      await button.dispatchEvent('click')
    }
    expect(movement.movementRequests).toHaveLength(0)
  })

  test('deja el ataque y el daño junto a los controles', async ({ page }) => {
    await prepareMovablePage(page, adjacentState)
    await page.route(`**/api/partidas/${activePartida.id}/ataque`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ estado: { ...adjacentState, vidaPersonajeActual: 74, vidaEnemigoActual: 15 }, danioInfligido: 15, danioRecibido: 10 }),
      })
    })

    await page.getByRole('button', { name: 'Atacar' }).tap()

    await expect(page.getByText('Golpeaste a Goblin por 15 de daño.')).toBeInViewport()
    await expect(page.getByRole('button', { name: 'Atacar' })).toBeInViewport()
  })
})
