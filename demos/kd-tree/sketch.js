let root;
let startPoints = 10;
let iterations = 1000;
let total = 0;
let tree, labels;
let stack = [];
let ptr = -1;

let bg = "#011F26";
let col_sel = "#025E73";
let col_left = "#03A688";
let col_right = "#F2668B";


function setup() {
  createCanvas(windowWidth, windowHeight);
  
  tree = createGraphics(width, height);
  labels = createGraphics(width, height);
  labels.textAlign(CENTER, TOP);
  labels.textSize(20);
  labels.strokeWeight(10);
  labels.stroke(bg);
  
  root = new Node(width/2, height/2);
  
  for (let i = 0; i < startPoints; i++) {
    addNode(root, new Node(width/8 + random(width/4*3), height/8 + random(height/4*3)));
  }
  
  print(`Construction took an average of ${total / startPoints} iterations per node`);
}


function draw() {
  background(bg);
  image(tree, 0, 0);
  
  if (!root) return;
  
  let target = [mouseX, mouseY];
  let best = nearestNeighbor(root, target);
  noFill();
  stroke(col_sel);
  strokeWeight(4);
  circle(target[0], target[1], 2 * sqrt(distSq(best.point, target)));
  line(best.point[0], best.point[1], target[0], target[1]);
  
  if (!keyIsDown(SHIFT)) {
    textAlign(CENTER, TOP);
    textSize(20);
    strokeWeight(10);
    stroke(bg);
    fill(col_sel);
    text(`(${target[0]}, ${target[1]})`, target[0], target[1] + 10);
    image(labels, 0, 0);
  }
}


function mousePressed() {
  addNode(root, new Node(mouseX, mouseY));
}


function addNode(node, newNode) {
  let coord, k;
  tree.strokeWeight(10);
  tree.stroke(col_sel);
  tree.point(newNode.point[0], newNode.point[1]);
  tree.strokeWeight(4);
  
  for (k = 0; k < iterations; k++) {
    coord = k%2;
    
    if (newNode.point[coord] < node.point[coord]) {
      if (node.left) {
        node = node.left;
      } else {
        node.left = newNode;
        tree.stroke(col_left);
        labels.fill(col_left);
        break;
      }
    } else {
      if (node.right) {
        node = node.right;
      } else {
        node.right = newNode;
        tree.stroke(col_right);
        labels.fill(col_right);
        break;
      }
    }
  }
  
  total += k;
  
  tree.line(node.point[0], node.point[1], newNode.point[0], newNode.point[1]);
  labels.text(`(${newNode.point[0]}, ${newNode.point[1]})`, newNode.point[0], newNode.point[1] + 10);
}

function nearestNeighbor(node, point) {
  let next, other, temp, k, coord;
  let best = node;
  stack = [];
  ptr = -1;
  stack[++ptr] = [node, 0];
  
  for (let i = 0; i < iterations; i++) {
    if (ptr == -1) {
      let iter = `${i} iteration(s)`;
      textAlign(LEFT, TOP);
      textSize(25);
      noFill();
      noStroke();
      rect(0, 0, textWidth(iter) + 20, 50);
      fill(col_sel);
      text(iter, 10, 10);
      
      break;
    }
    
    temp = stack[ptr--];
    node = temp[0];
    k = temp[1];
    coord = k%2;
    
    if (point[coord] < node.point[coord]) {
      next = node.left;
      other = node.right;
    } else {
      next = node.right;
      other = node.left;
    }
    
    if (next) {
      best = closestNode(point, best, next);
      stack[++ptr] = [next, k+1];
    }
    
    if (other) {
      let radiusSq = distSq(point, best.point);
      let dist = point[coord] - node.point[coord];
      
      if (radiusSq >= dist * dist) {
        best = closestNode(point, best, other);
        stack[++ptr] = [other, k+1];
      }
    }
  }
  
  return best;
}


function closestNode(p, n0, n1) {
  let d0 = distSq(p, n0.point);
  let d1 = distSq(p, n1.point);
  
  if (d0 < d1) {
    return n0;
  } else {
    return n1;
  }
}


function distSq(p0, p1) {
  return sq(p0[0] - p1[0]) + sq(p0[1] - p1[1]);
}


class Node {
  constructor(x, y) {
    this.point = [round(x), round(y)];
  }
}