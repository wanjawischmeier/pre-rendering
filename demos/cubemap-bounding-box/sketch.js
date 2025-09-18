let cubemapOrigins;

function pointSquareDistance(p, s) {
  return max(abs(p.x - s.x), abs(p.y - s.y));
}

function setup() {
  createCanvas(windowWidth, windowHeight);
  
  cubemapOrigins = [
    createVector(width * 0.2, height * 0.6),
    createVector(width * 0.4, height * 0.2),
    createVector(width * 0.7, height * 0.4),
  ];
  
  noFill();
}

function draw() {
  background(240);
  let r = 100;
  let mousePosition = createVector(mouseX, mouseY);
  
  stroke(150);
  for (let i = 0; i < cubemapOrigins.length; i++) {
    let p = cubemapOrigins[i];
    // let d = p5.Vector.dist(p, mousePosition);
    let d = pointSquareDistance(p, mousePosition);
    
    // fill(d < r ? 255 : 240);
    r = d + 50;
    strokeWeight(2);
    square(p.x - r, p.y - r, r * 2);
    
    strokeWeight(8);
    point(p);
  }
  
  strokeWeight(8);
  stroke(0);
  point(mousePosition);
}