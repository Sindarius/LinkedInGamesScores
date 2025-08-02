import { test } from '@playwright/test';

test.describe('Debug Tests', () => {
    test('should inspect admin form elements', async ({ page }) => {
        await page.goto('/admin');
        await page.waitForLoadState('networkidle');

        // Check all input elements and their attributes
        const inputElements = await page.locator('input').all();
        console.log('Number of input elements:', inputElements.length);

        for (let i = 0; i < inputElements.length; i++) {
            const input = inputElements[i];
            const type = await input.getAttribute('type');
            const id = await input.getAttribute('id');
            const className = await input.getAttribute('class');
            const placeholder = await input.getAttribute('placeholder');

            console.log(`Input ${i}:`, { type, id, className, placeholder });
        }

        // Check all button elements and their text content
        const buttonElements = await page.locator('button').all();
        console.log('Number of button elements:', buttonElements.length);

        for (let i = 0; i < buttonElements.length; i++) {
            const button = buttonElements[i];
            const text = await button.textContent();
            const className = await button.getAttribute('class');

            if (text?.includes('Login') || text?.includes('Logout')) {
                console.log(`Relevant Button ${i}:`, { text: text?.trim(), className });
            }
        }

        // Find password input by type instead of id
        const passwordInputs = await page.locator('input[type="password"]').all();
        console.log('Password inputs found:', passwordInputs.length);

        if (passwordInputs.length > 0) {
            const passwordInput = passwordInputs[0];
            const id = await passwordInput.getAttribute('id');
            const className = await passwordInput.getAttribute('class');
            console.log('Password input details:', { id, className });
        }
    });
});
