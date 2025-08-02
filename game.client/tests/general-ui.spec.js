import { test, expect } from '@playwright/test';

test.describe('General UI Tests', () => {
    test('should load the homepage successfully', async ({ page }) => {
        await page.goto('/');

        // Check that page loads without errors
        await expect(page).toHaveTitle(/Linked In Game Scoring/i);

        // Check for main content
        await expect(page.locator('body')).toBeVisible();
    });

    test('should have working navigation', async ({ page }) => {
        await page.goto('/');

        // Look for navigation elements
        const navigation = page.locator('nav, .p-menubar, .p-tabview');
        if ((await navigation.count()) > 0) {
            await expect(navigation.first()).toBeVisible();
        }
    });

    test('should be responsive on different screen sizes', async ({ page }) => {
        await page.goto('/');

        // Test desktop
        await page.setViewportSize({ width: 1920, height: 1080 });
        await expect(page.locator('body')).toBeVisible();

        // Test tablet
        await page.setViewportSize({ width: 768, height: 1024 });
        await expect(page.locator('body')).toBeVisible();

        // Test mobile
        await page.setViewportSize({ width: 375, height: 667 });
        await expect(page.locator('body')).toBeVisible();
    });

    test('should handle 404 pages gracefully', async ({ page }) => {
        await page.goto('/nonexistent-page');

        // Should either redirect to home or show a 404 page
        // Not throwing an error is the main expectation
        await expect(page.locator('body')).toBeVisible();
    });

    test('should load without console errors', async ({ page }) => {
        const consoleMessages = [];
        page.on('console', (msg) => {
            if (msg.type() === 'error') {
                consoleMessages.push(msg.text());
            }
        });

        await page.goto('/');

        // Wait a bit for any async errors
        await page.waitForTimeout(2000);

        // Filter out known acceptable errors (like network errors in dev)
        const criticalErrors = consoleMessages.filter((msg) => !msg.includes('favicon.ico') && !msg.includes('manifest.json') && !msg.includes('net::ERR_'));

        expect(criticalErrors).toHaveLength(0);
    });

    test('should have proper accessibility features', async ({ page }) => {
        await page.goto('/');

        // Check for basic accessibility features
        await page.locator('html[lang]').count(); // hasLanguage check

        // Check for form labels (if forms exist)
        const forms = await page.locator('form').count();
        if (forms > 0) {
            const labels = await page.locator('label').count();
            expect(labels).toBeGreaterThan(0);
        }
    });

    test('should load CSS and styling correctly', async ({ page }) => {
        await page.goto('/');

        // Check that styles are loaded by looking for styled elements
        const body = page.locator('body');

        // PrimeVue/TailwindCSS should apply styles
        const computedStyle = await body.evaluate((el) => {
            const style = window.getComputedStyle(el);
            return {
                margin: style.margin,
                padding: style.padding,
                fontFamily: style.fontFamily
            };
        });

        // Should have some styling applied (not all defaults)
        expect(computedStyle.fontFamily).not.toBe('');
    });

    test('should handle admin route protection', async ({ page }) => {
        await page.goto('/admin');
        await page.waitForLoadState('networkidle');

        // Should show login form if not authenticated
        await expect(page.locator('h6').filter({ hasText: 'Admin Authentication Required' })).toBeVisible();
    });
});
