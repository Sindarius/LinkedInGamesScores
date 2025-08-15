<script>
import { ref } from 'vue';
import { useToast } from 'primevue/usetoast';
import { createWorker } from 'tesseract.js';

export default {
    name: 'OCRTrainingTool',
    setup() {
        const toast = useToast();

        const currentImage = ref(null);
        const preprocessedImage = ref(null);
        const expectedText = ref('');
        const gameType = ref('time');
        const isAnalyzing = ref(false);
        const isTesting = ref(false);
        const analysisResults = ref([]);

        const gameTypes = [
            { label: 'Time-based (MM:SS)', value: 'time' },
            { label: 'Guess-based (3/6)', value: 'guess' }
        ];

        const handleTrainingImageUpload = (event) => {
            const file = event.files?.[0] || event.target.files?.[0];
            if (file) {
                currentImage.value = {
                    file,
                    preview: URL.createObjectURL(file)
                };

                // Reset previous results
                analysisResults.value = [];
                preprocessedImage.value = null;
            }
        };

        const preprocessImage = async (imageFile) => {
            return new Promise((resolve) => {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                const img = new Image();

                img.onload = () => {
                    // Scale down large images for better OCR processing
                    const maxDimension = 1200;
                    let { width, height } = img;

                    if (width > maxDimension || height > maxDimension) {
                        const scale = Math.min(maxDimension / width, maxDimension / height);
                        width = Math.round(width * scale);
                        height = Math.round(height * scale);
                    }

                    canvas.width = width;
                    canvas.height = height;

                    // Use better image scaling
                    ctx.imageSmoothingEnabled = true;
                    ctx.imageSmoothingQuality = 'high';
                    ctx.drawImage(img, 0, 0, width, height);

                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    const data = imageData.data;

                    // Enhanced preprocessing for large fonts
                    for (let i = 0; i < data.length; i += 4) {
                        const gray = data[i] * 0.299 + data[i + 1] * 0.587 + data[i + 2] * 0.114;

                        // Adaptive thresholding - more gentle for large fonts
                        let threshold = 128;

                        // Adjust threshold based on local brightness
                        const localBrightness = gray;
                        if (localBrightness > 200) {
                            threshold = 150; // Higher threshold for bright areas
                        } else if (localBrightness < 80) {
                            threshold = 100; // Lower threshold for dark areas
                        }

                        const contrast = gray > threshold ? 255 : 0;
                        data[i] = contrast;
                        data[i + 1] = contrast;
                        data[i + 2] = contrast;
                    }

                    ctx.putImageData(imageData, 0, 0);

                    canvas.toBlob((blob) => {
                        const url = URL.createObjectURL(blob);
                        preprocessedImage.value = url;
                        resolve(blob);
                    }, 'image/png');
                };

                img.src = URL.createObjectURL(imageFile);
            });
        };

        const testOCRWithConfig = async (imageFile, config) => {
            // Create worker with proper options for engine mode
            const workerOptions = {
                logger: (m) => console.log(`OCR Config ${config.name}:`, m)
            };

            // Handle OCR engine mode during worker creation
            let oem = 1; // default
            if (config.params.tessedit_ocr_engine_mode !== undefined) {
                oem = parseInt(config.params.tessedit_ocr_engine_mode);
            }

            const worker = await createWorker('eng', oem, workerOptions);

            try {
                // Set all other parameters (excluding engine mode)
                const runtimeParams = { ...config.params };
                delete runtimeParams.tessedit_ocr_engine_mode; // Remove to avoid error

                if (Object.keys(runtimeParams).length > 0) {
                    await worker.setParameters(runtimeParams);
                }

                const result = await worker.recognize(imageFile);
                return {
                    text: result.data.text.trim(),
                    confidence: Math.round(result.data.confidence),
                    config: config.name
                };
            } finally {
                await worker.terminate();
            }
        };

        const parseResult = (text, type) => {
            if (type === 'time') {
                const timeMatch = text.match(/(\d{1,2})[:\\/](\d{2})/);
                return timeMatch ? `${timeMatch[1]}:${timeMatch[2]}` : null;
            } else {
                const guessMatch = text.match(/(\d+)\/\d+/);
                return guessMatch ? `${guessMatch[1]}/6` : null;
            }
        };

        const analyzeImage = async () => {
            if (!currentImage.value) return;

            isAnalyzing.value = true;
            analysisResults.value = [];

            try {
                // Test current configuration
                const config = {
                    name: 'Current Config',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '6',
                        tessedit_ocr_engine_mode: '2'
                    }
                };

                const result = await testOCRWithConfig(currentImage.value.file, config);
                const parsed = parseResult(result.text, gameType.value);
                const isCorrect = parsed === expectedText.value.trim();

                analysisResults.value.push({
                    ...result,
                    parsed,
                    isCorrect,
                    rawText: result.text
                });

                // Also test with preprocessed image
                const preprocessed = await preprocessImage(currentImage.value.file);
                const preprocessedResult = await testOCRWithConfig(preprocessed, config);
                const preprocessedParsed = parseResult(preprocessedResult.text, gameType.value);
                const preprocessedCorrect = preprocessedParsed === expectedText.value.trim();

                analysisResults.value.push({
                    ...preprocessedResult,
                    config: 'Current Config (Preprocessed)',
                    parsed: preprocessedParsed,
                    isCorrect: preprocessedCorrect,
                    rawText: preprocessedResult.text
                });
            } catch (error) {
                console.error('Analysis failed:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Analysis Failed',
                    detail: error.message
                });
            } finally {
                isAnalyzing.value = false;
            }
        };

        const testMultipleParameters = async () => {
            if (!currentImage.value) return;

            isTesting.value = true;
            analysisResults.value = [];

            const configs = [
                {
                    name: 'Default',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '6',
                        tessedit_ocr_engine_mode: '2'
                    }
                },
                {
                    name: 'Large Font - Single Line',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '7',
                        tessedit_ocr_engine_mode: '2',
                        user_defined_dpi: '150', // Lower DPI for large fonts
                        textord_min_linesize: '2.5', // Adjust for large text
                        textord_excess_blobsize: '1.3'
                    }
                },
                {
                    name: 'Large Font - Legacy',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '7',
                        tessedit_ocr_engine_mode: '0',
                        user_defined_dpi: '150',
                        textord_min_linesize: '2.0'
                    }
                },
                {
                    name: 'Large Font - Single Word',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '8',
                        tessedit_ocr_engine_mode: '2',
                        user_defined_dpi: '150',
                        textord_min_linesize: '2.0',
                        classify_bln_numeric_mode: '1'
                    }
                },
                {
                    name: 'Large Font - Low DPI',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '6',
                        tessedit_ocr_engine_mode: '2',
                        user_defined_dpi: '100', // Very low DPI for very large fonts
                        textord_min_linesize: '1.5'
                    }
                },
                {
                    name: 'Single Column',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '4',
                        tessedit_ocr_engine_mode: '2'
                    }
                },
                {
                    name: 'Single Text Line',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '7',
                        tessedit_ocr_engine_mode: '2'
                    }
                },
                {
                    name: 'Single Word',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '8',
                        tessedit_ocr_engine_mode: '2'
                    }
                },
                {
                    name: 'Legacy Engine',
                    params: {
                        tessedit_char_whitelist: '0123456789:/.XDNF',
                        tessedit_pageseg_mode: '6',
                        tessedit_ocr_engine_mode: '0'
                    }
                }
            ];

            try {
                for (const config of configs) {
                    const result = await testOCRWithConfig(currentImage.value.file, config);
                    const parsed = parseResult(result.text, gameType.value);
                    const isCorrect = parsed === expectedText.value.trim();

                    analysisResults.value.push({
                        ...result,
                        parsed,
                        isCorrect,
                        rawText: result.text
                    });
                }

                // Sort by accuracy first, then confidence
                analysisResults.value.sort((a, b) => {
                    if (a.isCorrect && !b.isCorrect) return -1;
                    if (!a.isCorrect && b.isCorrect) return 1;
                    return b.confidence - a.confidence;
                });
            } catch (error) {
                console.error('Multi-parameter test failed:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Testing Failed',
                    detail: error.message
                });
            } finally {
                isTesting.value = false;
            }
        };

        const exportTrainingData = () => {
            const trainingData = {
                image: currentImage.value.preview,
                expectedText: expectedText.value,
                gameType: gameType.value,
                results: analysisResults.value,
                timestamp: new Date().toISOString()
            };

            const blob = new Blob([JSON.stringify(trainingData, null, 2)], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `ocr-training-${Date.now()}.json`;
            link.click();
            URL.revokeObjectURL(url);
        };

        return {
            currentImage,
            preprocessedImage,
            expectedText,
            gameType,
            gameTypes,
            isAnalyzing,
            isTesting,
            analysisResults,
            handleTrainingImageUpload,
            analyzeImage,
            testMultipleParameters,
            exportTrainingData
        };
    }
};
</script>

<template>
    <Card class="mt-4">
        <template #title>OCR Training & Debugging Tool</template>
        <template #content>
            <div class="space-y-4">
                <!-- Image Upload -->
                <div class="field">
                    <label>Upload Training Images</label>
                    <FileUpload ref="trainingUpload" mode="basic" accept="image/*" :maxFileSize="5000000" @select="handleTrainingImageUpload" :auto="false" chooseLabel="Choose Training Image" class="mb-2" />
                </div>

                <!-- Image Preview and Analysis -->
                <div v-if="currentImage" class="border rounded p-4">
                    <h3 class="text-lg font-semibold mb-3">Image Analysis</h3>

                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <!-- Original Image -->
                        <div>
                            <h4 class="font-medium mb-2">Original Image</h4>
                            <img :src="currentImage.preview" alt="Training image" class="max-w-full border rounded" />
                        </div>

                        <!-- Preprocessed Image -->
                        <div v-if="preprocessedImage">
                            <h4 class="font-medium mb-2">Preprocessed Image</h4>
                            <img :src="preprocessedImage" alt="Preprocessed" class="max-w-full border rounded" />
                        </div>
                    </div>

                    <!-- Expected vs Actual Results -->
                    <div class="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div>
                            <label class="block font-medium mb-2">Expected Text (what should be read)</label>
                            <InputText v-model="expectedText" placeholder="e.g., 1:23 or 4/6" class="w-full" />
                        </div>
                        <div>
                            <label class="block font-medium mb-2">Game Type</label>
                            <Select v-model="gameType" :options="gameTypes" optionLabel="label" optionValue="value" class="w-full" />
                        </div>
                    </div>

                    <!-- Analysis Controls -->
                    <div class="mt-4 flex gap-3">
                        <Button label="Analyze Image" icon="pi pi-search" @click="analyzeImage" :loading="isAnalyzing" size="small" />
                        <Button label="Test All Parameters" icon="pi pi-cog" @click="testMultipleParameters" :loading="isTesting" size="small" severity="secondary" />
                    </div>

                    <!-- Results -->
                    <div v-if="analysisResults.length > 0" class="mt-4">
                        <h4 class="font-medium mb-3">OCR Results</h4>
                        <div class="space-y-2">
                            <div v-for="(result, index) in analysisResults" :key="index" class="p-3 border rounded" :class="result.isCorrect ? 'border-green-500 bg-green-50' : 'border-red-500 bg-red-50'">
                                <div class="grid grid-cols-4 gap-2 text-sm">
                                    <div><strong>Config:</strong> {{ result.config }}</div>
                                    <div><strong>Raw Text:</strong> "{{ result.rawText }}"</div>
                                    <div><strong>Parsed:</strong> {{ result.parsed || 'Failed' }}</div>
                                    <div><strong>Confidence:</strong> {{ result.confidence }}%</div>
                                </div>
                                <div v-if="result.isCorrect" class="text-green-600 text-sm mt-1">✅ Correct! This configuration works well.</div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Training Data Export -->
                <div v-if="analysisResults.length > 0" class="mt-4">
                    <Button label="Export Training Data" icon="pi pi-download" @click="exportTrainingData" size="small" severity="help" />
                </div>
            </div>
        </template>
    </Card>
</template>
