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

async function prepareAuthenticatedPage(page) {
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
}

test.describe('Personajes', () => {
  test('muestra la selección y los personajes disponibles', async ({ page }) => {
    await prepareAuthenticatedPage(page)
    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Elegí tu personaje' })).toBeVisible()
    await expect(page.getByRole('button', { name: /Bruma/ })).toBeVisible()
    await expect(page.getByText('Guerrero')).toBeVisible()
    await expect(page.getByText('Nivel 1')).toBeVisible()
    await expect(page.getByText('100/100')).toBeVisible()
  })

  test('selecciona un personaje y conserva la selección al recargar', async ({ page }) => {
    await prepareAuthenticatedPage(page)
    await page.goto('/')

    await page.getByRole('button', { name: /Bruma/ }).click()
    await expect(page.getByText('Tu personaje activo es Bruma.')).toBeVisible()

    await page.reload()

    await expect(page.getByText('Tu personaje activo es Bruma.')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Bienvenido, usuario-e2e' })).toBeVisible()
  })
})
