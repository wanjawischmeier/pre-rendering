function setup() {
  createCanvas(windowWidth, windowHeight);
}

function draw() {
  background(250);
  fill(0);
  strokeWeight(4);
  
  let ax = width /2;  let ay = height /2;
  let bx = mouseX;    let by = mouseY;
  
  if (bx < ax) {
    let temp = ax;
    ax = bx;
    bx = temp;
    
    temp = ay;
    ay = by;
    by = temp;
  }
  
  let m = (ay - by) / (ax - bx);
  
  for (x = ax; x < bx; x++) {
    let y = m * (x - ax) + ay;
    
    point(x, y);
    print(x, y);
  }
}