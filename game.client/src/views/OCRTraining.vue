<script>
import OCRTrainingTool from '@/components/OCRTrainingTool.vue';

export default {
    name: 'OCRTraining',
    components: {
        OCRTrainingTool
    }
};
</script>

<template>
    <div class="grid">
        <div class="col-12">
            <div class="card">
                <h5>OCR Training & Debugging</h5>
                <p class="m-0">Use this tool to improve OCR accuracy for LinkedIn game screenshots. Upload your images, test different configurations, and find the optimal parameters.</p>
            </div>
        </div>

        <div class="col-12">
            <OCRTrainingTool />
        </div>

        <!-- Instructions and Tips -->
        <div class="col-12">
            <div class="card">
                <h5>How to Use This Tool</h5>
                <div class="grid">
                    <div class="col-12 md:col-6">
                        <h6>📸 Image Preparation Tips</h6>
                        <ul class="pl-4">
                            <li>Take clear, high-resolution screenshots</li>
                            <li>Ensure good contrast between text and background</li>
                            <li>Crop to focus on just the score/time area</li>
                            <li>Avoid blurry or angled screenshots</li>
                        </ul>
                    </div>
                    <div class="col-12 md:col-6">
                        <h6>🔧 Training Process</h6>
                        <ol class="pl-4">
                            <li>Upload a LinkedIn game screenshot</li>
                            <li>Enter the correct text that should be detected</li>
                            <li>Select the game type (time or guess-based)</li>
                            <li>Click "Test All Parameters" to find best config</li>
                            <li>Export training data for optimal parameters</li>
                        </ol>
                    </div>
                </div>
            </div>
        </div>

        <!-- Common Issues and Solutions -->
        <div class="col-12">
            <div class="card">
                <h5>Common OCR Issues & Solutions</h5>
                <div class="grid">
                    <div class="col-12 md:col-3">
                        <div class="surface-100 border-round p-3 h-full">
                            <h6 class="text-primary">❌ Large Fonts</h6>
                            <p class="text-sm">Big fonts appear as garbage text</p>
                            <p class="text-sm font-semibold text-green-600">Solution: Use "Large Font" configurations with lower DPI</p>
                        </div>
                    </div>
                    <div class="col-12 md:col-3">
                        <div class="surface-100 border-round p-3 h-full">
                            <h6 class="text-primary">❌ Wrong Numbers</h6>
                            <p class="text-sm">OCR reads "1:23" as "1.23" or "1 23"</p>
                            <p class="text-sm font-semibold text-green-600">Solution: Try "Single Text Line" mode</p>
                        </div>
                    </div>
                    <div class="col-12 md:col-3">
                        <div class="surface-100 border-round p-3 h-full">
                            <h6 class="text-primary">❌ Extra Characters</h6>
                            <p class="text-sm">OCR adds unwanted letters or symbols</p>
                            <p class="text-sm font-semibold text-green-600">Solution: Use character whitelist filtering</p>
                        </div>
                    </div>
                    <div class="col-12 md:col-3">
                        <div class="surface-100 border-round p-3 h-full">
                            <h6 class="text-primary">❌ Low Confidence</h6>
                            <p class="text-sm">OCR confidence below 70%</p>
                            <p class="text-sm font-semibold text-green-600">Solution: Try image preprocessing</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Large Font Optimization -->
        <div class="col-12">
            <div class="card">
                <h5>🔍 Large Font Optimization</h5>
                <div class="surface-50 border-round p-4 mb-4">
                    <p class="text-sm mb-3">
                        <strong>LinkedIn games often use large, bold fonts that challenge OCR systems.</strong>
                        The tool now includes specialized configurations for large font recognition.
                    </p>
                    <div class="grid">
                        <div class="col-12 md:col-4">
                            <h6 class="text-primary">📏 DPI Adjustment</h6>
                            <p class="text-xs">Lower DPI (100-150) tricks OCR into treating large fonts as normal size</p>
                        </div>
                        <div class="col-12 md:col-4">
                            <h6 class="text-primary">📐 Image Scaling</h6>
                            <p class="text-xs">Automatic downscaling to optimal dimensions (max 1200px)</p>
                        </div>
                        <div class="col-12 md:col-4">
                            <h6 class="text-primary">🎯 Adaptive Thresholding</h6>
                            <p class="text-xs">Smart contrast adjustment based on local brightness</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Parameter Explanations -->
        <div class="col-12">
            <div class="card">
                <h5>OCR Parameter Guide</h5>
                <div class="surface-100 border-round p-4">
                    <div class="grid">
                        <div class="col-12 md:col-4">
                            <h6>Page Segmentation Modes:</h6>
                            <ul class="text-sm pl-4">
                                <li><strong>Mode 4:</strong> Single column of text</li>
                                <li><strong>Mode 6:</strong> Single uniform block (default)</li>
                                <li><strong>Mode 7:</strong> Single text line ⭐ (best for large fonts)</li>
                                <li><strong>Mode 8:</strong> Single word ⭐ (best for isolated scores)</li>
                            </ul>
                        </div>
                        <div class="col-12 md:col-4">
                            <h6>OCR Engine Modes:</h6>
                            <ul class="text-sm pl-4">
                                <li><strong>Mode 0:</strong> Legacy engine ⭐ (often better for large fonts)</li>
                                <li><strong>Mode 2:</strong> LSTM only (modern, default)</li>
                                <li><strong>Mode 3:</strong> Combined Legacy + LSTM</li>
                            </ul>
                        </div>
                        <div class="col-12 md:col-4">
                            <h6>Large Font Parameters:</h6>
                            <ul class="text-sm pl-4">
                                <li><strong>user_defined_dpi:</strong> 100-150 for large fonts</li>
                                <li><strong>textord_min_linesize:</strong> Adjust line detection</li>
                                <li><strong>textord_excess_blobsize:</strong> Handle thick characters</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>
