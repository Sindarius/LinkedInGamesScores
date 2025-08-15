import { createWorker } from 'tesseract.js';

export class OCRService {
    constructor() {
        this.worker = null;
        this.isInitialized = false;
    }

    async initialize() {
        if (this.isInitialized) return;

        try {
            // Create worker with LSTM OCR engine (mode 2)
            this.worker = await createWorker('eng', 2, {
                logger: (m) => console.log('OCR Progress:', m)
            });

            // Configure for better recognition of numbers and time formats
            // Optimized for LinkedIn's game score display fonts
            await this.worker.setParameters({
                tessedit_char_whitelist: '0123456789:/.XDNF', // Include X, D, N, F for "DNF"
                tessedit_pageseg_mode: '6', // Single uniform block of text

                // Additional parameters for better accuracy with LinkedIn fonts
                preserve_interword_spaces: '1',
                user_defined_dpi: '300',
                tessedit_write_images: '0',

                // Confidence and quality settings
                classify_bln_numeric_mode: '1',
                tosp_old_to_method: '1',
                segment_segcost_rating: '1',
                enable_new_segsearch: '1',
                language_model_ngram_on: '1',

                // Character recognition improvements
                chop_enable: '1',
                use_new_state_cost: '1',
                segment_penalty_dict_frequent_word: '1',
                save_doc_words: '1'
            });

            this.isInitialized = true;
        } catch (error) {
            console.error('Failed to initialize OCR worker:', error);
            throw error;
        }
    }

    async preprocessImage(imageFile) {
        return new Promise((resolve) => {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            const img = new Image();

            img.onload = () => {
                // Set canvas size to image size
                canvas.width = img.width;
                canvas.height = img.height;

                // Draw the original image
                ctx.drawImage(img, 0, 0);

                // Get image data for processing
                const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                const data = imageData.data;

                // Convert to grayscale and increase contrast
                for (let i = 0; i < data.length; i += 4) {
                    // Calculate grayscale value
                    const gray = data[i] * 0.299 + data[i + 1] * 0.587 + data[i + 2] * 0.114;

                    // Increase contrast for better OCR (values closer to 0 or 255)
                    const contrast = gray > 128 ? 255 : 0;

                    data[i] = contrast; // Red
                    data[i + 1] = contrast; // Green
                    data[i + 2] = contrast; // Blue
                    // Alpha channel (data[i + 3]) stays the same
                }

                // Put the processed data back
                ctx.putImageData(imageData, 0, 0);

                // Convert canvas to blob
                canvas.toBlob(resolve, 'image/png');
            };

            img.src = URL.createObjectURL(imageFile);
        });
    }

    async extractTextFromImage(imageFile) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            // First try with the original image
            let result = await this.worker.recognize(imageFile);
            let text = result.data.text.trim();
            let confidence = result.data.confidence;

            console.log('OCR Result (original):', { text, confidence });

            // If confidence is low, try with preprocessed image
            if (confidence < 70) {
                console.log('Low confidence, trying with preprocessed image...');
                const preprocessedImage = await this.preprocessImage(imageFile);
                const preprocessedResult = await this.worker.recognize(preprocessedImage);

                if (preprocessedResult.data.confidence > confidence) {
                    text = preprocessedResult.data.text.trim();
                    confidence = preprocessedResult.data.confidence;
                    console.log('OCR Result (preprocessed):', { text, confidence });
                }
            }

            return text;
        } catch (error) {
            console.error('OCR text extraction failed:', error);
            throw error;
        }
    }

    parseLinkedInGameScore(text, gameType) {
        const cleanText = text.replace(/\s+/g, ' ').trim();
        console.log('Parsing text:', cleanText, 'for game type:', gameType);

        if (gameType === 'time') {
            return this.parseTimeScore(cleanText);
        } else if (gameType === 'guess') {
            return this.parseGuessScore(cleanText);
        }

        return null;
    }

    parseTimeScore(text) {
        // Look for time patterns like "1:23", "0:45", "2:15", etc.
        const timePatterns = [
            /(\d{1,2}):(\d{2})/g, // Standard MM:SS format
            /(\d{1,2})\/(\d{2})/g, // Sometimes OCR reads : as /
            /(\d{1,2})\s*:\s*(\d{2})/g, // With possible spaces
            /(\d{1,2})\s*\/\s*(\d{2})/g // With possible spaces and /
        ];

        for (const pattern of timePatterns) {
            const matches = [...text.matchAll(pattern)];
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
        const numbers = text.match(/\d+/g);
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

    parseGuessScore(text) {
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
            const matches = [...text.matchAll(pattern)];
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
            if (pattern.test(text)) {
                return {
                    guessCount: 99, // Special value for DNF
                    isDNF: true,
                    confidence: 'high'
                };
            }
        }

        // Try to find any single number that could be a guess count
        // LinkedIn games typically use 1-8 guesses
        const numbers = text.match(/\d+/g);
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

    // Enhanced method for training/debugging purposes
    async extractTextWithDetailedResults(imageFile, customParams = null) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            // Apply custom parameters if provided
            if (customParams) {
                await this.worker.setParameters(customParams);
            }

            const result = await this.worker.recognize(imageFile);

            return {
                text: result.data.text.trim(),
                confidence: result.data.confidence,
                words: result.data.words,
                lines: result.data.lines,
                paragraphs: result.data.paragraphs,
                symbols: result.data.symbols,
                bbox: result.data.bbox
            };
        } catch (error) {
            console.error('Detailed OCR extraction failed:', error);
            throw error;
        }
    }

    // Method to test multiple configurations
    async testMultipleConfigurations(imageFile, configurations) {
        const results = [];

        for (const config of configurations) {
            try {
                // Get OCR engine mode (default to 2 if not specified)
                const oem = config.params.tessedit_ocr_engine_mode || 2;

                const tempWorker = await createWorker('eng', oem);

                // Set parameters excluding the engine mode
                const runtimeParams = { ...config.params };
                delete runtimeParams.tessedit_ocr_engine_mode;

                if (Object.keys(runtimeParams).length > 0) {
                    await tempWorker.setParameters(runtimeParams);
                }

                const result = await tempWorker.recognize(imageFile);
                results.push({
                    configName: config.name,
                    text: result.data.text.trim(),
                    confidence: result.data.confidence,
                    params: config.params
                });

                await tempWorker.terminate();
            } catch (error) {
                console.error(`Configuration ${config.name} failed:`, error);
                results.push({
                    configName: config.name,
                    text: '',
                    confidence: 0,
                    error: error.message,
                    params: config.params
                });
            }
        }

        return results;
    }

    // Method to create training data
    createTrainingData(imageFile, expectedText, actualResults) {
        return {
            image: URL.createObjectURL(imageFile),
            expectedText,
            results: actualResults,
            timestamp: new Date().toISOString(),
            recommendations: this.generateRecommendations(actualResults, expectedText)
        };
    }

    generateRecommendations(results, expectedText) {
        const recommendations = [];

        // Find the best performing configuration
        const sortedResults = results
            .filter((r) => !r.error)
            .sort((a, b) => {
                // Prioritize correct results, then confidence
                const aCorrect = this.isTextMatch(a.text, expectedText);
                const bCorrect = this.isTextMatch(b.text, expectedText);

                if (aCorrect && !bCorrect) return -1;
                if (!aCorrect && bCorrect) return 1;
                return b.confidence - a.confidence;
            });

        if (sortedResults.length > 0) {
            const best = sortedResults[0];
            recommendations.push(`Best configuration: ${best.configName} (${best.confidence}% confidence)`);

            if (this.isTextMatch(best.text, expectedText)) {
                recommendations.push('✅ Found working configuration!');
                recommendations.push(`Recommended parameters: ${JSON.stringify(best.params, null, 2)}`);
            } else {
                recommendations.push('❌ No configuration produced correct results');
                recommendations.push('Consider: image preprocessing, cropping, or different segmentation modes');
            }
        }

        return recommendations;
    }

    isTextMatch(actual, expected) {
        // Normalize both strings for comparison
        const normalize = (str) => str.replace(/\s+/g, '').toLowerCase();
        return normalize(actual) === normalize(expected);
    }

    async terminate() {
        if (this.worker) {
            await this.worker.terminate();
            this.worker = null;
            this.isInitialized = false;
        }
    }
}

// Export a singleton instance
export const ocrService = new OCRService();
