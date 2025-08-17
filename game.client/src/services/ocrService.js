// Complete Tesseract.js Implementation
import Tesseract from 'tesseract.js';

// Default configuration settings

// OCR Engine Mode meanings:
// 0: Legacy engine only
// 1: Neural nets LSTM engine only
// 2: Legacy + LSTM engines
// 3: Default (based on language)

// Page Segmentation Mode meanings:
// 6: Assume a single uniform block of text (most common)
// 7: Treat the image as a single text line
// 8: Treat the image as a single word
// 11: Sparse text - find as much text as possible

async function recognizeText(imageFile) {
    // Create worker (modern API - comes pre-loaded)
    const worker = await Tesseract.createWorker('eng');

    try {
        // Set configuration parameters - always use engine mode 3 and pageseg mode 1, no character whitelisting
        await worker.setParameters({
            tessedit_ocr_engine_mode: '3',
            tessedit_pageseg_mode: '1'
        });

        // Always preprocess image with background removal (tolerance 32)
        const img = new Image();
        const imageUrl = URL.createObjectURL(imageFile);

        const preprocessedBlob = await new Promise((resolve) => {
            img.onload = async () => {
                try {
                    const processedCanvas = preprocessImage(img);
                    processedCanvas.toBlob(resolve, 'image/png');
                } catch (error) {
                    resolve(null); // Fallback to original on preprocessing error
                } finally {
                    URL.revokeObjectURL(imageUrl);
                }
            };
            img.onerror = () => resolve(null);
            img.src = imageUrl;
        });

        // Use preprocessed image if available, fallback to original
        const imageToProcess = preprocessedBlob || imageFile;

        // Recognize text
        const {
            data: { text, confidence }
        } = await worker.recognize(imageToProcess);

        return { text, confidence };
    } catch (error) {
        console.error('OCR Error:', error);
        throw error;
    } finally {
        await worker.terminate();
    }
}

// Usage example:
// const result = await recognizeText(imageFile);

// Image Preprocessing Function
function preprocessImage(imageElement) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // Set canvas size to match image
    canvas.width = imageElement.naturalWidth;
    canvas.height = imageElement.naturalHeight;

    // Draw original image
    ctx.drawImage(imageElement, 0, 0);

    // Get image data
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;

    // Processing settings - default background removal tolerance
    const colorTolerance = 35;

    // Background removal preprocessing
    const mostFrequentColor = findMostFrequentColor(data, colorTolerance);
    applyBackgroundRemoval(data, mostFrequentColor, colorTolerance);

    // Put processed image data back to canvas
    ctx.putImageData(imageData, 0, 0);

    return canvas;
}

// Find the most frequent color in the image
function findMostFrequentColor(data, tolerance) {
    const colorCounts = new Map();

    // Sample every 4th pixel for performance
    const sampleRate = 4;

    for (let i = 0; i < data.length; i += 4 * sampleRate) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        // Quantize colors to reduce variation
        const quantizeStep = Math.max(1, Math.floor(tolerance / 2));
        const quantizedR = Math.floor(r / quantizeStep) * quantizeStep;
        const quantizedG = Math.floor(g / quantizeStep) * quantizeStep;
        const quantizedB = Math.floor(b / quantizeStep) * quantizeStep;

        const colorKey = `${quantizedR},${quantizedG},${quantizedB}`;
        colorCounts.set(colorKey, (colorCounts.get(colorKey) || 0) + 1);
    }

    // Find the most frequent color
    let maxCount = 0;
    let mostFrequentColor = null;

    for (const [colorKey, count] of colorCounts) {
        if (count > maxCount) {
            maxCount = count;
            const [r, g, b] = colorKey.split(',').map(Number);
            mostFrequentColor = { r, g, b };
        }
    }

    return mostFrequentColor;
}

// Apply background removal
function applyBackgroundRemoval(data, backgroundColor, tolerance) {
    if (!backgroundColor) return;

    for (let i = 0; i < data.length; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        // Calculate color distance from background color
        const colorDistance = Math.sqrt(Math.pow(r - backgroundColor.r, 2) + Math.pow(g - backgroundColor.g, 2) + Math.pow(b - backgroundColor.b, 2));

        // If color is similar to background, make it white
        // Otherwise, make it black
        if (colorDistance <= tolerance) {
            data[i] = 255; // R = white
            data[i + 1] = 255; // G = white
            data[i + 2] = 255; // B = white
        } else {
            data[i] = 0; // R = black
            data[i + 1] = 0; // G = black
            data[i + 2] = 0; // B = black
        }
    }
}

// Usage with Tesseract:
async function recognizeWithPreprocessing(imageFile) {
    // Load image into img element
    const img = new Image();
    const imageUrl = URL.createObjectURL(imageFile);

    return new Promise((resolve, reject) => {
        img.onload = async () => {
            try {
                // Preprocess image
                const processedCanvas = preprocessImage(img);

                // Convert canvas to blob for Tesseract
                const processedBlob = await new Promise((resolve) => {
                    processedCanvas.toBlob(resolve, 'image/png');
                });

                // Run OCR on processed image
                const result = await recognizeText(processedBlob);
                resolve(result);
            } catch (error) {
                reject(error);
            } finally {
                URL.revokeObjectURL(imageUrl);
            }
        };
        img.onerror = reject;
        img.src = imageUrl;
    });
}

// Helper methods for LinkedIn game score parsing
function parseLinkedInGameScore(text, gameType) {
    // Split text into lines and try parsing each line
    const lines = text
        .split(/\r?\n/)
        .map((line) => line.trim())
        .filter((line) => line.length > 0);

    // Try parsing the full text first (existing behavior)
    const cleanText = text.replace(/\s+/g, ' ').trim();
    let result = null;

    if (gameType === 'time') {
        result = parseTimeScore(cleanText);
    } else if (gameType === 'guess') {
        result = parseGuessScore(cleanText);
    }

    // If full text parsing failed, try each line individually
    if (!result) {
        for (const line of lines) {
            if (gameType === 'time') {
                result = parseTimeScore(line);
            } else if (gameType === 'guess') {
                result = parseGuessScore(line);
            }

            if (result) {
                break;
            }
        }
    }

    return result;
}

function parseTimeScore(text) {
    // Clean up "solved in" text and trim
    const cleanText = text.replace(/solved\s+in\s*/gi, '').trim();

    // Look for time patterns like "1:23", "0:45", "2:15", etc.
    const timePatterns = [
        /(\d{1,2}):(\d{2})/g, // Standard MM:SS format
        /(\d{1,2})\/(\d{2})/g, // Sometimes OCR reads : as /
        /(\d{1,2})\s*:\s*(\d{2})/g, // With possible spaces
        /(\d{1,2})\s*\/\s*(\d{2})/g // With possible spaces and /
    ];

    for (const pattern of timePatterns) {
        const matches = [...cleanText.matchAll(pattern)];
        for (const match of matches) {
            const minutes = parseInt(match[1]);
            const seconds = parseInt(match[2]);

            // Validate the time makes sense
            if (minutes >= 0 && minutes < 60 && seconds >= 0 && seconds < 60) {
                return {
                    minutes,
                    seconds,
                    confidence: 'high'
                };
            }
        }
    }

    // Try to find individual numbers that might be minutes/seconds
    const numbers = cleanText.match(/\d+/g);
    if (numbers && numbers.length >= 2) {
        const minutes = parseInt(numbers[0]);
        const seconds = parseInt(numbers[1]);

        if (minutes >= 0 && minutes < 60 && seconds >= 0 && seconds < 60) {
            return {
                minutes,
                seconds,
                confidence: 'medium'
            };
        }
    }

    return null;
}

function parseGuessScore(text) {
    // Clean up "solved in" text and trim
    const cleanText = text.replace(/solved\s+in\s*/gi, '').trim();

    // Enhanced LinkedIn-specific guess count patterns
    const guessPatterns = [
        /(\d+)\s*\/\s*[6789]/g, // LinkedIn format like "3/6", "4/6", "5/7", etc.
        /(\d+)\s*\/\s*\d+/g, // General format like "3/6" or "4/6"
        /(\d+)\s*guesses?/gi, // "3 guesses" or "4 guess"
        /attempt\s*(\d+)/gi, // "attempt 3"
        /try\s*(\d+)/gi, // "try 3"
        /(\d+)\s*tries/gi, // "3 tries"
        /solve.*?(\d+)/gi, // "solved in 3"
        /finished.*?(\d+)/gi, // "finished in 4"
        /completed.*?(\d+)/gi // "completed in 5"
    ];

    for (const pattern of guessPatterns) {
        const matches = [...cleanText.matchAll(pattern)];
        for (const match of matches) {
            const guesses = parseInt(match[1]);

            // Validate guess count makes sense (1-10 typically for LinkedIn games)
            if (guesses >= 1 && guesses <= 10) {
                return {
                    guessCount: guesses,
                    confidence: 'high'
                };
            }
        }
    }

    // Enhanced DNF patterns specific to LinkedIn games
    const dnfPatterns = [
        /d\s*n\s*f/gi, // "D N F" with possible spaces
        /dnf/gi,
        /did\s*not\s*finish/gi,
        /failed/gi,
        /x\s*\/\s*\d+/gi, // Pattern like "X/6" or "X / 6"
        /\dx\s*\/\s*\d+/gi, // Pattern like "6X/6"
        /no\s*solution/gi,
        /couldn.?t\s*solve/gi,
        /ran\s*out/gi // "ran out of guesses"
    ];

    for (const pattern of dnfPatterns) {
        if (pattern.test(cleanText)) {
            return {
                guessCount: 99, // Special value for DNF
                isDNF: true,
                confidence: 'high'
            };
        }
    }

    // Try to find any single number that could be a guess count
    // LinkedIn games typically use 1-8 guesses
    const numbers = cleanText.match(/\d+/g);
    if (numbers && numbers.length > 0) {
        for (const numStr of numbers) {
            const num = parseInt(numStr);
            if (num >= 1 && num <= 8) {
                return {
                    guessCount: num,
                    confidence: 'medium'
                };
            }
        }
    }

    return null;
}

// Wrapper function to match the existing API
async function extractTextFromImage(imageFile) {
    try {
        const result = await recognizeText(imageFile);
        return result.text;
    } catch (error) {
        console.error('OCR text extraction failed:', error);
        throw error;
    }
}

// Function to test OCR with custom configuration
async function testOCRWithConfig(imageFile, config) {
    // Always use OCR engine mode 3 (ignore config settings)
    const worker = await Tesseract.createWorker('eng', 3);

    try {
        // Set configuration parameters - force engine mode 3 and pageseg mode 1, remove character whitelisting
        const runtimeParams = { ...config.params };
        // Always override these settings regardless of config
        runtimeParams.tesseract_ocr_engine_mode = '3';
        runtimeParams.tessedit_pageseg_mode = '1';
        // Remove character whitelisting to get full context
        delete runtimeParams.tessedit_char_whitelist;

        await worker.setParameters(runtimeParams);

        // Always preprocess image with background removal (tolerance 32)
        const img = new Image();
        const imageUrl = URL.createObjectURL(imageFile);

        const preprocessedBlob = await new Promise((resolve) => {
            img.onload = async () => {
                try {
                    const processedCanvas = preprocessImage(img);
                    processedCanvas.toBlob(resolve, 'image/png');
                } catch (error) {
                    resolve(null); // Fallback to original on preprocessing error
                } finally {
                    URL.revokeObjectURL(imageUrl);
                }
            };
            img.onerror = () => resolve(null);
            img.src = imageUrl;
        });

        // Use preprocessed image if available, fallback to original
        const imageToProcess = preprocessedBlob || imageFile;

        // Recognize text
        const {
            data: { text, confidence }
        } = await worker.recognize(imageToProcess);

        return {
            text: text.trim(),
            confidence: Math.round(confidence),
            config: config.name
        };
    } catch (error) {
        console.error('OCR Error:', error);
        throw error;
    } finally {
        await worker.terminate();
    }
}

// Function to test multiple configurations
async function testMultipleConfigurations(imageFile, configurations) {
    const results = [];

    for (const config of configurations) {
        try {
            const result = await testOCRWithConfig(imageFile, config);
            results.push(result);
        } catch (error) {
            console.error(`Configuration ${config.name} failed:`, error);
            results.push({
                config: config.name,
                text: '',
                confidence: 0,
                error: error.message
            });
        }
    }

    return results;
}

// Function to get preprocessed image URL for display
async function getPreprocessedImageUrl(imageFile) {
    const img = new Image();
    const imageUrl = URL.createObjectURL(imageFile);

    return new Promise((resolve) => {
        img.onload = () => {
            try {
                const processedCanvas = preprocessImage(img);
                processedCanvas.toBlob((blob) => {
                    const url = URL.createObjectURL(blob);
                    resolve(url);
                }, 'image/png');
            } catch (error) {
                resolve(null);
            } finally {
                URL.revokeObjectURL(imageUrl);
            }
        };
        img.onerror = () => resolve(null);
        img.src = imageUrl;
    });
}

// Create OCR service object to match existing usage pattern
const ocrService = {
    extractTextFromImage,
    parseLinkedInGameScore,
    testOCRWithConfig,
    testMultipleConfigurations,
    getPreprocessedImageUrl
};

// Export the main functions and service object
export { recognizeText, preprocessImage, recognizeWithPreprocessing, parseLinkedInGameScore, extractTextFromImage, ocrService };
