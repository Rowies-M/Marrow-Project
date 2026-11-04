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

  await page.goto('/')
  await page.getByRole('button', { name: /Bruma/ }).click()
  await expect(page.getByRole('heading', { name: 'Tus partidas' })).toBeVisible()
}

test.describe('Pantalla de juego', () => {
  test('accede desde una partida activa y muestra la escena visual', async ({ page }) => {
    await preparePage(page, [activePartida])

    await page.getByRole('button', { name: 'Jugar' }).click()

    await expect(page.getByRole('heading', { name: 'La entrada a la mazmorra' })).toBeVisible()
    await expect(page.getByLabel('Guerrero Bruma')).toBeVisible()
    await expect(page.getByLabel('Goblin')).toBeVisible()
    await expect(page.getByRole('region', { name: 'HUD del Guerrero' })).toContainText('Vida: 100/100')
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
})
