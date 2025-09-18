let v0, v1, v2, v3;
let maxTop, maxBottom, textOffset;

// Based on: http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html
function rasterizeTopFlatTriangle(v0, v1, v2)
{
    if (v0.y >= v1.y)
        return;
        
    let sliderAbsolute = (maxTop.value() / 100) * (v1.y - v0.y);
    let maxIterations = v0.y + sliderAbsolute;

    let invslope1 = (v1.x - v0.x) / (v1.y - v0.y);
    let invslope2 = (v2.x - v0.x) / (v2.y - v0.y);

    let curx1 = v0.x;
    let curx2 = v0.x;
  
    for (let scanlineY = v0.y; scanlineY <= min(v1.y, maxIterations); scanlineY++)
    {
        line(curx1, scanlineY, curx2, scanlineY);
      
        curx1 += invslope1;
        curx2 += invslope2;
    }
}

function rasterizeBottomFlatTriangle(v0, v1, v2)
{ 
    if (v0.y < v1.y)
        return;
    
    let sliderAbsolute = (maxBottom.value() / 100) * (v0.y - v1.y);
    let maxIterations = v0.y - v1.y - sliderAbsolute;

    let invslope1 = (v0.x - v1.x) / (v0.y - v1.y);
    let invslope2 = (v0.x - v2.x) / (v0.y - v2.y);

    let curx1 = v0.x;
    let curx2 = v0.x;
  
    for (let scanlineY = v0.y; scanlineY > v1.y + maxIterations; scanlineY--)
    {
        line(curx1, scanlineY, curx2, scanlineY);
      
        curx1 -= invslope1;
        curx2 -= invslope2;
    }
}

function rasterizeTriangle(v0, v1, v2) {
  let v3 = createVector(v0.x + ((v1.y - v0.y) / (v2.y - v0.y)) * (v2.x - v0.x), v1.y);
  
  strokeWeight(1);
  stroke(125, 200, 150);
  rasterizeTopFlatTriangle(v0, v1, v3);
  
  strokeWeight(1);
  stroke(100, 150, 200);
  rasterizeBottomFlatTriangle(v2, v1, v3);
  
  strokeWeight(2);
  stroke(200);
  line(v0.x, v0.y, v1.x, v1.y);
  line(v1.x, v1.y, v2.x, v2.y);
  line(v2.x, v2.y, v0.x, v0.y);
  drawingContext.setLineDash([5, 5]);
  line(v3.x, v3.y, v1.x, v1.y);
  drawingContext.setLineDash([]);
  
  strokeWeight(6);
  stroke(100);
  point(v0);
  point(v1);
  point(v2);
  stroke(150);
  point(v3);
  
  noStroke();
  fill(100);
  text('v0', v0.x + textOffset.x, v0.y + textOffset.y);
  
  noStroke();
  fill(100);
  text('v1', v1.x + textOffset.x, v1.y + textOffset.y);
  
  noStroke();
  fill(100);
  text('v2', v2.x + textOffset.x, v2.y + textOffset.y);
  
  noStroke();
  fill(100);
  text('v3', v3.x + textOffset.x, v3.y + textOffset.y);
}

function setup() {
  createCanvas(400, 400);
  textSize(18);
  
  v0 = createVector(150, 100);
  v1 = createVector(275, 225);
  v2 = createVector(225, 300);
  v3 = createVector(100, 250);
  
  maxTop = createSlider(0, 100, 100);
  maxBottom = createSlider(0, 100, 100);
  
  textOffset = createVector(10, -5);
}

function draw() {
  background(240);
  
  rasterizeTriangle(v0, v1, v2);
  rasterizeTriangle(v0, v3, v2);
}