# FitMyBike  🚴
A **C#** application for basic bike‑fit analysis using camera input and ONNX person finder and pose‑estimation models.

FitMyBike enables quick and simple rider‑position analysis based on photos or video recordings. It combines pose estimation, geometric calculations, and an intuitive interface to help cyclists and bike‑fitters evaluate bike setup.

---

## ✨ Features

### 📸 Image and Video Processing  
FitMyBike loads rider photos or videos and extracts key body‑position information.

- Detects joint points (hips, knees, ankles, shoulders) using ONNX models  
- Draws overlays, angle lines, and markers directly on the image  
- Tracks posture frame‑by‑frame  
- Allows manual correction of detected points  
- Provides a simple spin‑stage visualization for video analysis  

---

## 📐 Angle and Geometry Calculations  
Once joints are detected, the app computes the most important bike‑fit angles:

- Knee extension angle  
- Hip angle  
- Torso angle  
- Arm reach angle  

These measurements help determine whether the rider is too stretched, too cramped, or misaligned.

---

## 🧩 ONNX Model Integration  
The application uses ONNX Runtime to run lightweight pose‑estimation models locally.

- Loads ONNX models  
- Runs inference on images  
- Converts model output into usable joint coordinates  

---

## 🖥️ UI and Workflow  
FitMyBike provides a simple workflow from loading media to exporting results.

- Image and video input management  
- Spin‑stage visualization  
- Manual point adjustment  
- Measurement overlays and angle lines  

---

## 🛠️ Tech Stack

- **C#**
- **ML.NET**
- **ONNX Runtime**  
- **WPF** 
- **OpenCV (OpenCVSharp) **

---

## 📄 Licenses for used ONNX models

**ViT Pose** -> https://github.com/ViTAE-Transformer/ViTPose/blob/main/LICENSE
---
https://huggingface.co/docs/transformers/model_doc/vitpose

**Person finder** -> https://huggingface.co/Xenova/yolos-base
---
https://huggingface.co/docs/transformers/tasks/object_detection
