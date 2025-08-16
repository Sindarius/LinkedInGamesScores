<script>
import { ref } from 'vue';
import { useToast } from 'primevue/usetoast';
import { ocrService } from '@/services/ocrService.js';

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
                
                // Generate preprocessed image from service
                if (currentImage.value?.file) {
                    ocrService.getPreprocessedImageUrl(currentImage.value.file).then(url => {
                        preprocessedImage.value = url;
                    });
                }
            }
        };

        const testOCRWithConfig = ocrService.testOCRWithConfig;

        const parseResult = (text, type) => {
            const parsed = ocrService.parseLinkedInGameScore(text, type);
            if (parsed) {
                if (type === 'time') {
                    return `${parsed.minutes}:${parsed.seconds.toString().padStart(2, '0')}`;
                } else if (type === 'guess') {
                    return parsed.isDNF ? 'DNF' : `${parsed.guessCount}/6`;
                }
            }
            return null;
        };

        const analyzeImage = async () => {
            if (!currentImage.value) return;

            isAnalyzing.value = true;
            analysisResults.value = [];

            try {
                // Test current configuration - using fixed OCR settings, no character whitelisting
                const config = {
                    name: 'Current Config',
                    params: {
                        tessedit_pageseg_mode: '1',
                        tessedit_ocr_engine_mode: '3'
                    }
                };

                // Test current configuration (preprocessing now handled automatically in service)
                const result = await testOCRWithConfig(currentImage.value.file, config);
                const parsed = parseResult(result.text, gameType.value);
                const isCorrect = parsed === expectedText.value.trim();

                analysisResults.value.push({
                    ...result,
                    parsed,
                    isCorrect,
                    rawText: result.text
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

            // All configs now use fixed OCR engine mode 3 and pageseg mode 1, no character whitelisting for full context
            const configs = [
                {
                    name: 'Default (No Restrictions)',
                    params: {
                        tessedit_pageseg_mode: '1',
                        tessedit_ocr_engine_mode: '3'
                    }
                },
                {
                    name: 'Lower DPI',
                    params: {
                        tessedit_pageseg_mode: '1',
                        tessedit_ocr_engine_mode: '3',
                        user_defined_dpi: '150'
                    }
                },
                {
                    name: 'Very Low DPI',
                    params: {
                        tessedit_pageseg_mode: '1',
                        tessedit_ocr_engine_mode: '3',
                        user_defined_dpi: '100'
                    }
                },
                {
                    name: 'High DPI',
                    params: {
                        tessedit_pageseg_mode: '1',
                        tessedit_ocr_engine_mode: '3',
                        user_defined_dpi: '300'
                    }
                },
                {
                    name: 'Adjusted Line Size',
                    params: {
                        tessedit_pageseg_mode: '1',
                        tessedit_ocr_engine_mode: '3',
                        textord_min_linesize: '2.0'
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
                            <h4 class="font-medium mb-2">Preprocessed Image (Background Removal - Tolerance 32)</h4>
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
                        <div class="space-y-3">
                            <div v-for="(result, index) in analysisResults" :key="index" class="p-4 border rounded" :class="result.isCorrect ? 'border-green-500 bg-green-50' : 'border-red-500 bg-red-50'">
                                <!-- Header row with config, parsed result, and confidence -->
                                <div class="grid grid-cols-3 gap-4 text-sm mb-3">
                                    <div><strong>Config:</strong> {{ result.config }}</div>
                                    <div><strong>Parsed:</strong> {{ result.parsed || 'Failed' }}</div>
                                    <div><strong>Confidence:</strong> {{ result.confidence }}%</div>
                                </div>
                                
                                <!-- Raw text in its own section with proper multiline handling -->
                                <div class="text-sm">
                                    <div class="font-semibold mb-2">Raw Text:</div>
                                    <div class="bg-gray-100 p-2 rounded border font-mono text-xs whitespace-pre-wrap break-words max-h-32 overflow-y-auto">{{ result.rawText || '(empty)' }}</div>
                                </div>
                                
                                <div v-if="result.isCorrect" class="text-green-600 text-sm mt-2">✅ Correct! This configuration works well.</div>
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
