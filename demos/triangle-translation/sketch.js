function preload() {
  font = loadFont("MomcakeBold-WyonA.otf");
}

function setup() {
  cnv = createCanvas(windowWidth, windowHeight);
  
  outline = color(25, 38, 64);
  bg = color(242, 178, 155);
  circl = color(191, 63, 87);
  T1 = color(242, 232, 94);
  T2 = color(122, 159, 191);
  
  textAlign(CENTER);
  noCursor();
  angleMode(DEGREES);
  textFont(font);
  textSize(15);
  stroke(outline);
  cnv.mouseWheel(updateLat);
  
  cx = width/2;
  cy = height/2;
  if(width <= height) d = width / 1.5;
  if(height < width) d = height / 1.5;
  
  lat = 270;
  gamma1 = 90;
}

function draw() {
  background(bg);
  
  // Initial values
  b1 = mouseX-cx; a1 = mouseY-cy;
  Ax = 0; Ay = 0;
  Bx = b1; By = a1;
  
  
  /*               T1               */
  /*--------------------------------*/
  // Missing angles
  beta1 = atan(b1/a1);
  if(a1 < 0)  alph1 = 180-180-beta1-gamma1;
  else         alph1 = 180-beta1-gamma1;
  
  // Missing sides
  c = sqrt((Bx**2)+(By**2));
  
  // Point C1
  C1x = b1; C1y = 0;
  /*________________________________*/
  
  
  /*               T2               */
  /*--------------------------------*/
  // Missing beta2 and b2
  beta2 = lat;
  b2 = d/2;
  
  // Other missing angles
  if(a1 < 0) beta3 = 90-beta1-beta2;
  else       beta3 = 270-beta1-beta2;
  gamma2 = asin((sin(beta2)/b2)*c);
  alph2 = 180-beta2-gamma2;
  
  // Other missing side
  a2 = (b2/sin(beta2))*sin(alph2)
  
  // Point C2
  C2x = Bx+a2*cos(beta3)
  C2y = By+a2*sin(beta3)
  /*________________________________*/
  
  // Center points
  Ax += cx; Ay += cy;
  Bx += cx; By += cy;
  C1x += cx; C1y += cy;
  C2x += cx; C2y += cy;
  
  
  
  /*             Circle             */
  /*--------------------------------*/
  fill(circl);
  strokeWeight(4);
  stroke(outline);
  
  circle(cx, cy, d);
  /*________________________________*/
  
  
  /*            Triangle            */
  /*--------------------------------*/
  strokeWeight(2);
  
  fill(T1);
  triangle(Ax, Ay, Bx, By, C1x, C1y);
  
  fill(T2);
  triangle(Ax, Ay, Bx, By, C2x, C2y);
  /*________________________________*/
  
  
  /*              Line              */
  /*--------------------------------*/  
  txt = "a1 = l".replace("l", round(a1.toString(), 1));
  debugLine(Bx, By, C1x, C1y, txt);
  
  txt = "a2 = l".replace("l", round(a2.toString(), 1));
  debugLine(Bx, By, C2x, C2y, txt);
  
  txt = "b1 = l".replace("l", round(b1.toString(), 1));
  debugLine(Ax, Ay, C1x, C1y, txt);
  
  txt = "b2 = l".replace("l", round(b2.toString(), 1));
  debugLine(Ax, Ay, C2x, C2y, txt);
  
  txt = "c = l".replace("l", round(c.toString(), 1));
  debugLine(Ax, Ay, Bx, By, txt);
  /*________________________________*/
  
  
  /*             Points             */
  /*--------------------------------*/
  b = 5;
  
  txt = "A (angle2, angle1)".replace("angle1", round(alph1*(-1).toString(), 1)).replace("angle2", round(alph2*(-1).toString(), 1));
  debugPoint(Ax, Ay, txt);
  
  txt = "B (angle)".replace("angle", round(360-beta2.toString(), 1));
  bbox = font.textBounds(txt, Bx, By);
  fill(bg);
  strokeWeight(2);
  stroke(outline)
  rect(bbox.x-b, bbox.y-b*2.5, bbox.w+b*2, bbox.h+b*4);
  fill(outline);
  strokeWeight(0);
  text(txt, Bx, By-b-2);
  strokeWeight(10);
  point(Bx, By);
  
  txt = "C1 (angle)".replace("angle", round(gamma1.toString(), 1));
  debugPoint(C1x, C1y, txt);
  
  txt = "C2 (angle)".replace("angle", round(gamma2*(-1).toString(), 1));
  debugPoint(C2x, C2y, txt);
  /*________________________________*/
}

function updateLat(event) {
  lat += event.deltaY/20;
}

function debugPoint(x, y, description, b=5) {
  bbox = font.textBounds(description, x, y);
  fill(outline);
  strokeWeight(0);
  rect(bbox.x-b, bbox.y-b*2.5, bbox.w+b*2, bbox.h+b*4);
  fill(bg);
  stroke(bg);
  text(txt, x, y-b-2);
  strokeWeight(10);
  point(x, y);
}
function debugLine(x1, y1, x2, y2, description, b=5) {
  x = (x1+x2)/2;
  y = (y1+y2)/2;
  bbox = font.textBounds(description, x, y);
  fill(bg);
  strokeWeight(1);
  rect(bbox.x-b, bbox.y-b, bbox.w+b*2, bbox.h+b*2);
  fill(0);
  stroke(bg);
  text(txt, x, y);
  stroke(outline);
}