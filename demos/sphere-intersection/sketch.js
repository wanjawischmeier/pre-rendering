let cx; let cy;

function setup() {
  createCanvas(windowWidth, windowHeight);
  
  cx = width / 2;
  cy = height / 2;
  
}

function draw() {
  background(255);
  
  stroke(0); fill(255)
  strokeWeight(4)
  circle(cx, cy, height / 1.5);
  
  stroke(100, 200, 100); fill(0);
  strokeWeight(2);
  circle(cx, cy, 10)
}

function intersection(
  rox, roy, roz, rtx, rty, rtz,
  sx, sy, sz, sr
) {
  Vector3 o_minus_c = rayPos - spherePos;

  let p = Vector3.Dot(rayDir, o_minus_c);
  let q = Vector3.Dot(o_minus_c, o_minus_c) - (sphereRadius * sphereRadius);

        float discriminant = (p * p) - q;
        if (discriminant < 0.0f)
        {
            return 0;
        }

        float dRoot = Mathf.Sqrt(discriminant);
        dist1 = -p - dRoot;
        dist2 = -p + dRoot;

        return (discriminant > 1e-7) ? 2 : 1;
}