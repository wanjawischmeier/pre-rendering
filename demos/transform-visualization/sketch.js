let objX = 350;
let objY = 250;
let objW = 50;
let objSW = 3;
let cubeSize = 300;
let mx, my, mdx, mdy;

function setup() {
  createCanvas(windowWidth, windowHeight);
  mx = width / 2 - 100;
  my = height / 2 + 50;
  
  textAlign(CENTER);
}

function draw() {
  background(220);
  if (mouseIsPressed && width / 2 - mouseX > height / 2 - mouseY) {
    mx = mouseX;
    my = mouseY;
    mdx = width / 2 - mx;
    mdy = height / 2 - my;
    print("x:" + mdx + ", y:" + mdy);
  }
  
  // cubemap
  noStroke();
  fill(250);
  square(width / 2 - cubeSize, height / 2 - cubeSize, cubeSize * 2);
  
  fill(200, 250, 200);
  triangle(width / 2, height / 2, width / 2 - cubeSize, height / 2 - cubeSize, width / 2 + cubeSize, height / 2 - cubeSize);
  
  fill(250, 200, 200);
  triangle(width / 2, height / 2, width / 2 + cubeSize, height / 2 - cubeSize, width / 2 + cubeSize, height / 2 + cubeSize);
  
  noFill();
  stroke(100);
  strokeWeight(8);
  square(width / 2 - cubeSize, height / 2 - cubeSize, cubeSize * 2);
  
  // original line
  noFill();
  stroke(100);
  strokeWeight(4);
  for (let x = objX; x < objX + objW * objSW; x += objSW) {
    point(x, objY);
  }
  
  // transformed line
  stroke(0);
  let tx = objX + mdx;
  let ty = objY + mdy;
  for (let ox = 0; ox < objW * objSW; ox += objSW) {
    point(tx + ox, ty);
  }
  
  stroke(150, 200, 200);
  strokeWeight(2);
  // real distance
  line(width / 2, height / 2, objX + objW * objSW / 2, objY);
  line(width / 2, height / 2, tx + objW * objSW / 2, ty);
  
  // unprojected distance
  line(width / 2 - 2, height / 2, width / 2 - 2, objY);
  line(width / 2 + 2, height / 2, width / 2 + 2, ty);
  
  noFill();
  stroke(0);
  strokeWeight(1);
  for (let x = objX; x < objX + objW * objSW; x += objSW) {
    let od = height / 2 - objY;
    line(x, height / 2 - cubeSize, x, height / 2 - cubeSize - od / 10);
  }
  
  for (let ox = 0; ox < objW * objSW; ox += objSW) {
    let td = height / 2 - ty;
    line(tx + ox, height / 2 - cubeSize, tx + ox, height / 2 - cubeSize - td / 10);
  }
  
  // player and origin
  stroke(100, 100, 100);
  strokeWeight(10);
  point(width / 2, height / 2);
  
  stroke(100, 200, 100);
  point(mx, my);
  
  fill(0);
  noStroke();
  text("Origin", width / 2, height / 2 + 20);
  text("Player", mx, my + 20);
}
