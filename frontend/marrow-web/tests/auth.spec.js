import { test, expect } from '@playwright/test'

test.describe('Autenticación', () => {
  test('muestra el login sin sesión', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Entrá a tu aventura' })).toBeVisible()
    await expect(page.getByRole('textbox', { name: 'Usuario o email' })).toBeVisible()
    await expect(page.getByRole('textbox', { name: 'Contraseña' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Iniciar sesión' })).toBeVisible()
  })

  test('valida los campos obligatorios del login', async ({ page }) => {
    await page.goto('/')

    await page.getByRole('button', { name: 'Iniciar sesión' }).click()

    await expect(page.getByRole('textbox', { name: 'Usuario o email' })).toHaveAttribute('required', '')
    await expect(page.getByRole('textbox', { name: 'Contraseña' })).toHaveAttribute('required', '')
  })

  test('muestra el error de credenciales inválidas', async ({ page }) => {
    await page.route('**/api/auth/login', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'Credenciales inválidas.' }),
      })
    })

    await page.goto('/')
    await page.getByRole('textbox', { name: 'Usuario o email' }).fill('usuario-inexistente')
    await page.getByRole('textbox', { name: 'Contraseña' }).fill('contraseña-de-prueba')
    await page.getByRole('button', { name: 'Iniciar sesión' }).click()

    await expect(page.getByRole('alert')).toContainText('Credenciales inválidas.')
    await expect(page.getByRole('button', { name: 'Iniciar sesión' })).toBeEnabled()
  })
})
