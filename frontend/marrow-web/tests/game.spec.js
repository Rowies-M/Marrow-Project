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
  clase: 0,
}

const activePartida = {
  id: 17,
  fechaCreacion: '2026-11-03T15:00:00Z',
  fechaUltimaActualizacion: '2026-11-03T15:00:00Z',
  estado: 0,
  personajeId: character.id,
}

const finalizedPartida = { ...activePartida, id: 18, estado: 1 }

const gameState = {
  partidaId: activePartida.id,
  fechaCreacion: activePartida.fechaCreacion,
  fechaUltimaActualizacion: activePartida.fechaUltimaActualizacion,
  estado: activePartida.estado,
  personajeId: character.id,
  personajeNombre: character.nombre,
  personajeNivel: character.nivel,
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
})
