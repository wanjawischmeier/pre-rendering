let gridSize = 10;  // Grid resolution
let texels = [];    // 2D array storing texel data
let depthThreshold = 0.7; // Depth threshold for discontinuities
let subsquareBorder = 5;

function setup() {
    let gridCellWidth = min(windowWidth, windowHeight);
    createCanvas(gridCellWidth, gridCellWidth);
    noSmooth();
    generateTexels();
}

function generateTexels() {
    for (let y = 0; y < gridSize; y++) {
        texels[y] = [];
        for (let x = 0; x < gridSize; x++) {
            texels[y][x] = {
                r: floor(random(2)) * 255,
                g: 0,
                b: 0,
                a: random(1), // Depth between 0 and 1
                detailLevel: floor(random(0))  // Random discrete detail level
            };
        }
    }
}

function draw() {
    background(150, 0, 150);
    drawTexels();
    
    // Sample the texture at the mouse position
    let uv = createVector(mouseX / width, mouseY / height);
    let sampledColor = sampleSmartBilinear(uv);
    if (sampledColor == null) return;
    
    // Draw sampled color
    // fill(sampledColor.r, sampledColor.a * 255, 0);
    // stroke(255);
    // rect(mouseX, mouseY, 20, 20);
}

// Draw the grid of texels
function drawTexels() {
    let cellSize = width / gridSize;
    for (let y = 0; y < gridSize; y++) {
        for (let x = 0; x < gridSize; x++) {
            // draw grid
            stroke(255);
            strokeWeight(2);
            noFill();
            square(x * cellSize, y * cellSize, cellSize);
          
            // sample texels
            let xOff = (x + 1) % gridSize;
            let yOff = (y + 1) % gridSize;
            let t00 = texels[y][x];
            let t10 = texels[y][xOff];
            let t01 = texels[yOff][x];
            let t11 = texels[yOff][xOff];
          
            let x0 = x * cellSize;
            let y0 = y * cellSize;
            let x1 = x * cellSize + cellSize / 2;
            let y1 = y * cellSize + cellSize / 2;
            let x2 = x0 + cellSize;
            let y2 = y0 + cellSize;
          
            let c00_10 = areTexelsContinuous(t00, t10);
            let c00_01 = areTexelsContinuous(t00, t01);
            let c10_11 = areTexelsContinuous(t10, t11);
            let c01_11 = areTexelsContinuous(t01, t11);
          
            // Count continuous neighbors for each texel
            let count00 = c00_10 + c00_01;
            let count10 = c00_10 + c10_11;
            let count01 = c00_01 + c01_11;
            let count11 = c10_11 + c01_11;

            // Find the texel with the most continuous neighbors
            let maxCount = max(count00, count10, count01, count11);
            let dominantTexel = t00;
            if (count10 == maxCount) dominantTexel = t10;
            if (count01 == maxCount) dominantTexel = t01;
            if (count11 == maxCount) dominantTexel = t11;

            renderSubsquare(x0, y0, x1, y1, t00, t10, t01, c00_10, c00_01, dominantTexel, 0);
            renderSubsquare(x1, y0, x2, y1, t10, t00, t11, c00_10, c10_11, dominantTexel, 1);
            renderSubsquare(x1, y1, x2, y2, t11, t10, t01, c10_11, c01_11, dominantTexel, 2);
            renderSubsquare(x0, y1, x1, y2, t01, t00, t11, c00_01, c01_11, dominantTexel, 3);
          
            stroke(0, 200, 0);
            strokeWeight(2);
            if (c00_10) {
              line(x * cellSize, y * cellSize + subsquareBorder, x * cellSize + cellSize, y * cellSize + subsquareBorder);
            }
            if (c00_01) {
              line(x * cellSize + subsquareBorder, y * cellSize, x * cellSize + subsquareBorder, y * cellSize + cellSize);
            }
            if (c10_11) {
              line(x * cellSize + cellSize - subsquareBorder, y * cellSize, x * cellSize + cellSize - subsquareBorder, y * cellSize + cellSize);
            }
            if (c01_11) {
              line(x * cellSize, y * cellSize + cellSize - subsquareBorder, x * cellSize + cellSize, y * cellSize + cellSize - subsquareBorder);
            }
            
            noStroke();
            fill(t00.r * 0.9, t00.r * 0.6, 0);
            circle(x * cellSize, y * cellSize, cellSize  / 2);
            fill(0, t00.a * t00.detailLevel * (255/5), 0);
            circle(x * cellSize, y * cellSize, cellSize  / 4);
        }
    }
}

function areTexelsContinuous(t1, t2) {
    if (!t1 || !t2) return false;
    if (t1.r != t2.r) return false;
    return true;
}


function renderSubsquare(x0, y0, x1, y1, texel, neighbor1, neighbor2, isContinuous1, isContinuous2, dominantTexel, quadrant) {
    let cellSize = width / gridSize;
    noStroke();

    // Rule 1: If any neighbor is continuous, fill the entire subsquare
    if (isContinuous1 || isContinuous2) {
        fill(texel.r * 0.9, texel.r * 0.4, 0);
        square(x0, y0, cellSize / 2);
        return;
    }

    // Rule 2: Otherwise, split into two right-angled triangles
    fill(texel.r * 0.9, texel.r * 0.6, 0);

    // Determine the correct triangle orientation for each quadrant
    if (quadrant === 0) {
        // Top-left subsquare
        triangle(x0, y0, x1, y0, x0, y1);
    } else if (quadrant === 1) {
        // Top-right subsquare
        triangle(x0, y0, x1, y0, x1, y1);
    } else if (quadrant === 2) {
        // Bottom-right subsquare
        triangle(x0, y1, x1, y0, x1, y1);
    } else if (quadrant === 3) {
        // Bottom-left subsquare
        triangle(x0, y0, x1, y1, x0, y1);
    }

    // Fill the inner triangle with the dominant color
    fill(dominantTexel.r * 0.9, dominantTexel.r * 0.5, 0);
    
    if (quadrant === 0) {
        triangle(x0, y1, x1, y0, x1, y1);
    } else if (quadrant === 1) {
        triangle(x0, y0, x1, y1, x0, y1);
    } else if (quadrant === 2) {
        triangle(x0, y0, x1, y0, x0, y1);
    } else if (quadrant === 3) {
        triangle(x0, y0, x1, y0, x1, y1);
    }
}


// Function to sample bilinear texture with continuity checks
function sampleSmartBilinear(uv) {
    let x = uv.x * gridSize;
    let y = uv.y * gridSize;
    
    if (x + 1 >= gridSize || y + 1 >= gridSize) return null;
    
    let x0 = floor(x);
    let x1 = min(x0 + 1, gridSize);
    let y0 = floor(y);
    let y1 = min(y0 + 1, gridSize);
    
    fill(255);
    textSize(18);
    text("(" + x0 + ", " + y0 + ")", mouseX, mouseY);
    
    let f = createVector(x - x0, y - y0); // Fractional part for interpolation
    
    let t00 = texels[y0][x0];
    let t10 = texels[y0][x1];
    let t01 = texels[y1][x0];
    let t11 = texels[y1][x1];
    
    // Check continuity
    let h01 = areTexelsContinuous(t00, t01);
    let h10 = areTexelsContinuous(t10, t11);
    let v00 = areTexelsContinuous(t00, t10);
    let v11 = areTexelsContinuous(t01, t11);
  
    return t00;
}