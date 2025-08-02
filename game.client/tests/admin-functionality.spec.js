import { test, expect } from '@playwright/test';

test.describe('Admin Panel Functionality', () => {
    test.beforeEach(async ({ page }) => {
        // Login to admin panel before each test
        await page.goto('/admin');
        await page.waitForLoadState('networkidle');
        await page.fill('input[type="password"]', 'admin123');
        await page.click('button >> text=Login');
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
    });

    test('should switch between Games and Scores management tabs', async ({ page }) => {
        // Check Games Management tab is visible by default
        await expect(page.getByRole('tab', { name: 'Games Management' })).toBeVisible();

        // Click on Scores Management tab
        await page.getByRole('tab', { name: 'Scores Management' }).click();

        // Verify we're on the scores tab
        await expect(page.getByRole('tab', { name: 'Scores Management' })).toBeVisible();

        // Click back to Games Management
        await page.getByRole('tab', { name: 'Games Management' }).click();
        await expect(page.getByRole('tab', { name: 'Games Management' })).toBeVisible();
    });

    test('should display games management table', async ({ page }) => {
        // Should be on Games Management tab by default
        await expect(page.getByRole('tab', { name: 'Games Management' })).toBeVisible();

        // Check for Games Management specific content (header text)
        await expect(page.locator('h6').filter({ hasText: 'Games Management' })).toBeVisible();

        // Check that we can see the Add Game button (specific to games table)
        await expect(page.locator('button').filter({ hasText: 'Add Game' })).toBeVisible();
    });

    test('should display scores management table', async ({ page }) => {
        // Click on Scores Management tab
        await page.getByRole('tab', { name: 'Scores Management' }).click();

        // Wait for the scores management content to be visible
        await expect(page.locator('h6').filter({ hasText: 'Scores Management' })).toBeVisible();

        // Check that we can see the Add Score button (specific to scores table)
        await expect(page.locator('button').filter({ hasText: 'Add Score' })).toBeVisible();
    });

    test('should show correct page title and header', async ({ page }) => {
        // Check page has correct title structure
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
        await expect(page.locator('p').filter({ hasText: 'Manage games and score entries' })).toBeVisible();
    });

    test('should maintain responsive layout on mobile', async ({ page }) => {
        // Set mobile viewport
        await page.setViewportSize({ width: 375, height: 667 });

        // Check that admin panel is still functional
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
        await expect(page.getByRole('tab', { name: 'Games Management' })).toBeVisible();

        // Switch tabs should still work
        await page.getByRole('tab', { name: 'Scores Management' }).click();
        await expect(page.getByRole('tab', { name: 'Scores Management' })).toBeVisible();
    });

    test('should handle navigation to admin panel directly when authenticated', async ({ page }) => {
        // We're already authenticated from beforeEach
        // Navigate away and back
        await page.goto('/');
        await page.goto('/admin');
        await page.waitForLoadState('networkidle');

        // Should still be authenticated and see admin panel
        await expect(page.locator('h5').filter({ hasText: 'Admin Panel' })).toBeVisible();
        await expect(page.locator('button').filter({ hasText: 'Logout' })).toBeVisible();
    });
});
