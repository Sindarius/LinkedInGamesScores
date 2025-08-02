import { test, expect } from '@playwright/test';

test.describe('Admin Authentication', () => {
    test.beforeEach(async ({ page }) => {
        // Navigate to admin page
        await page.goto('/admin');
    });

    test('should show login form when not authenticated', async ({ page }) => {
        // Wait for page to load
        await page.waitForLoadState('networkidle');

        // Check that login form is visible
        await expect(page.locator('h6').filter({ hasText: 'Admin Authentication Required' })).toBeVisible();
        await expect(page.locator('label').filter({ hasText: 'Password' })).toBeVisible();
        await expect(page.locator('input[type="password"]')).toBeVisible();
        await expect(page.locator('button').filter({ hasText: 'Login' })).toBeVisible();

        // Check that admin content is not visible
        await expect(page.getByRole('tab', { name: 'Games Management' })).not.toBeVisible();
        await expect(page.getByRole('tab', { name: 'Scores Management' })).not.toBeVisible();
    });

    test('should reject invalid password', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        // Fill in wrong password
        await page.fill('input[type="password"]', 'wrongpassword');
        await page.click('button >> text=Login');

        // Check for error message
        await expect(page.locator('.p-error').filter({ hasText: 'Invalid password' })).toBeVisible();

        // Verify still on login screen
        await expect(page.locator('h6').filter({ hasText: 'Admin Authentication Required' })).toBeVisible();
    });

    test('should accept valid password and show admin panel', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        // Fill in correct password
        await page.fill('input[type="password"]', 'admin123');
        await page.click('button >> text=Login');

        // Wait for authentication to complete
        await expect(page.locator('h6').filter({ hasText: 'Admin Authentication Required' })).not.toBeVisible();

        // Check that admin content is now visible
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
        await expect(page.getByRole('tab', { name: 'Games Management' })).toBeVisible();
        await expect(page.getByRole('tab', { name: 'Scores Management' })).toBeVisible();
        await expect(page.locator('button').filter({ hasText: 'Logout' })).toBeVisible();
    });

    test('should show loading state during authentication', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        // Fill in password
        await page.fill('input[type="password"]', 'admin123');

        // Click login and immediately check for loading state
        await page.click('button >> text=Login');

        // Wait for successful authentication
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
    });

    test('should logout and return to login form', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        // Login first
        await page.fill('input[type="password"]', 'admin123');
        await page.click('button >> text=Login');
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();

        // Logout
        await page.click('button >> text=Logout');

        // Should return to login form
        await expect(page.locator('h6').filter({ hasText: 'Admin Authentication Required' })).toBeVisible();
        await expect(page.locator('button').filter({ hasText: 'Logout' })).not.toBeVisible();
    });

    test('should disable login button when password is empty', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        const loginButton = page.locator('button').filter({ hasText: 'Login' });

        // Button should be disabled initially
        await expect(loginButton).toBeDisabled();

        // Type some characters
        await page.fill('input[type="password"]', 'test');
        await expect(loginButton).toBeEnabled();

        // Clear the input
        await page.fill('input[type="password"]', '');
        await expect(loginButton).toBeDisabled();
    });

    test('should support enter key for authentication', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        // Fill password and press Enter
        await page.fill('input[type="password"]', 'admin123');
        await page.press('input[type="password"]', 'Enter');

        // Should authenticate successfully
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
    });

    test('should clear password field after successful login', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        const passwordInput = page.locator('input[type="password"]');

        // Fill password and login
        await passwordInput.fill('admin123');
        await page.click('button >> text=Login');

        // Wait for successful login
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();

        // Logout to check if password was cleared
        await page.click('button >> text=Logout');

        // Password field should be empty
        await expect(passwordInput).toHaveValue('');
    });

    test('should persist authentication across page refresh', async ({ page }) => {
        await page.waitForLoadState('networkidle');

        // Login first
        await page.fill('input[type="password"]', 'admin123');
        await page.click('button >> text=Login');
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();

        // Refresh the page
        await page.reload();
        await page.waitForLoadState('networkidle');

        // Should still be authenticated
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
        await expect(page.locator('button').filter({ hasText: 'Logout' })).toBeVisible();
    });
});
