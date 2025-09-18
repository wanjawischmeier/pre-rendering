let resolution = 40;
let latitude, frequency, selected, offset;
let bg = "#CF9BF2";
let dark = "#1F1659";
let mid = "#5005F2";
let light = "#F25D50";

function convertLongitude(latitude_, longitude, distance) {
  return [
    distance * sin(latitude_) * sin(longitude),
    distance * cos(longitude),
    distance * cos(latitude_) * sin(longitude)
  ];
}

function debugPoint(iteration, previous, current, next) {
  stroke(lerpColor(color(light), color(dark), iteration / resolution));
  strokeWeight(width / 50);
  point(current);
  strokeWeight(width / 100);
  line(current.x, current.y, next.x, next.y);
  
  // the normal vector (used as extrapolated surface)
  let n = p5.Vector.sub(previous, next);
  let center = createVector(width/2, height/2);
  // player translation
  let player = p5.Vector.add(center, offset);
  // point translation
  let relative = p5.Vector.add(current, offset);
  
  // ray originating from player
  let as = player;
  let ad = p5.Vector.sub(relative, player);
  // ray normal
  let bs = p5.Vector.add(current, 0);
  let bd = n;
  let dx = bs.x - as.x;
  let dy = bs.y - as.y;
  let det = bd.x * ad.y - bd.y * ad.x;
  let u = (dy * bd.x - dx * bd.y) / det;
  point(as.x + ad.x * u, as.y + ad.y * u);
  
  strokeWeight(width / 200);
  line(current.x, current.y, current.x + dx / 4, current.y + dy / 4);
  n = p5.Vector.div(n, 4);
  line(current.x - n.x, current.y - n.y, current.x + n.x, current.y + n.y);
  n = p5.Vector.mult(p5.Vector.div(n, p5.Vector.mag(n)), width / 20);
  line(current.x, current.y, current.x + n.y, current.y - n.x);
}

function selectedPoint(coords, depth, normal) {
  let center = createVector(width/2, height/2);
  let player = p5.Vector.add(center, offset);
  let relative = p5.Vector.add(coords, offset);
  
  let as = player;
  let ad = p5.Vector.sub(center, coords); // alternatively (relative, player)
  let bs = coords;
  let bd = normal;
  let dx = bs.x - as.x;
  let dy = bs.y - as.y;
  let det = bd.x * ad.y - bd.y * ad.x;
  let u = (dy * bd.x - dx * bd.y) / det;
  
  stroke(mid);
  line(center.x, center.y, coords.x, coords.y);
  line(center.x, center.y, player.x, player.y);
  // line(coords.x, coords.y, coords.x - normal.x + offset.x, coords.y - normal.y + offset.y);
  line(coords.x - normal.x, coords.y - normal.y, coords.x + normal.x, coords.y + normal.y);
  line(player.x, player.y, relative.x, relative.y);
  
  strokeWeight(width / 50);
  point(center);
  point(player);
  point(as.x + ad.x * u, as.y + ad.y * u);
}

function setup() {
  createCanvas(800, windowHeight);
  strokeWeight(10);
  latitude = createSlider(1e-10, PI - 1e-10, HALF_PI - 1e-10, 0);
  latitude.position(10, 10);
  frequency = createSlider(1, round(resolution/2), round(resolution/10), 0);
  frequency.position(200, 10);
  selected = createSlider(1, resolution - 2, round(resolution/10), 1);
  selected.position(390, 10);
  offset = createVector(width/10, 0);
}

function draw() {
  background(bg);
  
  if (mouseIsPressed && mouseY > 40) {
    offset = createVector(mouseX - width/2, mouseY - height/2);
  }
  
  let first, second, previous, previousPrevious;
  for (let i = 0; i < resolution; i++) {
    longitude = i / resolution * TWO_PI - HALF_PI;
    distance = width/3 + width/10 * sin(longitude * frequency.value());
    let coords = convertLongitude(longitude, latitude.value(), distance);
    current = createVector(round(width/2 + coords[2]), height/2 + coords[0]);
    
    if (i == 0) {
      first = current;
    }
    else if (i == 1) {
      second = current;
    }
    else if (i > 1) {
      debugPoint(i, previousPrevious, previous, current);
    }
    if (i == selected.value() + 1) {
      let n = p5.Vector.sub(current, previousPrevious);
      selectedPoint(previous, distance, n);
    }
    previousPrevious = previous;
    previous = current;
  }
  debugPoint(resolution, previousPrevious, previous, first);
  debugPoint(0, previous, first, second);
}