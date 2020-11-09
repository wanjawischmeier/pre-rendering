let c; let r;
let w; let h;
let col_d; let col;

function cx(x) { return (width/2)+x; }
function cy(y) { return (height/2)+y; }

function setup() {
    createCanvas(windowWidth -10, windowHeight -40);
    background(250);
    rectMode(CENTER);
    noStroke();

    c = 40;
    w = width/c;
    h = height/c;
    r = c/2;

    let needed = getNeeded(r);
    col_d = 255/needed.length;
    let i = 0;

    needed.forEach(element => {
        col = i*col_d;

        fill(255-col/2, col/2, col);
        rect(cx(element[0]*w), cy(element[1]*h), w, h);

        i++;
    });
}

function getNeeded(r) {
    let n = [];

    // fill(255, 0, 0);
    n.push([0, 0]);
        
    for (x = 1; x < r; x++) {
        
        n.push([ x,  0]);
        n.push([-x,  0]);
        n.push([ 0,  x]);
        n.push([ 0, -x]);
        
        for (y = 1; y < r; y++) {
            col = (x+y)*col_d;
            // fill(255-col/2, col/2, col);
            n.push([ x,  y]);
            n.push([-x,  y]);
            n.push([ x, -y]);
            n.push([-x, -y]);
        }
    }

    return n;
}