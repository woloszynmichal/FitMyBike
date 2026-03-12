**FitMyBike** is a C# application designed to support basic, camera‑based bike‑fit analysis.


Its functions revolve around three core areas:

📸 1. Video and image processing

The app lets you load rider photos or videos and extract key body‑position information. Typical functions include:

Detecting joint points (hips, knees, ankles, shoulders) using ONNX pose‑estimation models.

Drawing overlays and angle lines directly on the image.

Tracking rider posture frame‑by‑frame for consistency.

Adjusting manualy detected points for each frame.

Provides an overview of spin stage and lets you analyze your vids in a simple way.


📐 2. Angle and geometry calculations

Once the pose is detected, the app computes the angles most relevant to bike fitting, such as:

  Knee extension angle
  
  Hip angle
  
  Torso angle
  
  Arm reach angle
  
These calculations help identify whether the rider is too stretched, too cramped, or misaligned.


🚲 3. Fit guidance and comparison

The tool can compare measured angles against typical recommended ranges for different riding styles (e.g., road, gravel, MTB). Functions often include:

Highlighting angles that fall outside recommended ranges

Suggesting which bike components might need adjustment (saddle height, setback, handlebar reach)

Allowing before/after comparison when testing different setups


🧩 4. ONNX model integration

The project uses ONNX runtime to run lightweight pose‑estimation models locally. Functions handle:

Loading the model

Running inference on images

Converting model output into usable joint coordinates


🖥️ 5. UI and workflow helpers

The app includes functions for:

Managing image/video input

Vizualizing spin stage

Manually moving detected points and adjusting them as needed
Displaying measurement overlays
Saving annotated images
Providing a simple workflow from loading → analyzing → exporting results
