# GFace Trainer Module
## GFace Proof of Capability

Identity training PoC for GFace. Capture faces, assign a name and group, and upload to API.

OpenCV for initial image processing and face extraction, Microsoft Cognitive Services for face identification, grouping, and validation.

### Controls
#### Camera
- Set Camera
    - Exposure - exposure compensation
    - FPS - frames per second
- Camera - turn the camera on/off
- Single - add current frame to image array
- Live Faces - identify, extract, and save face from current captured frame
- Record as Stream - add frames from webcam stream to image array

#### Capture
- Capture - grab current frame for identification/comparison
- Live faces - grab frames from webcam stream for identification/comparison

#### Training
- Set Names - store as local variables and create local folders for:
    - Person Name - name/identifier of person used for identification (Face API)
    - Group Name - name/identifier of group used for identification (Face API)
- Grab Faces - Extract faces from stream using OpenCV
- Load to trainer - Load extracted faces to Face API trainer (async)
- Train - Train Face API using loaded faces, with names and groups (async)
- Identify - Attempt to identify the faces captured using the Capture window, based on the trained data

## NuGet Packages
- Microsoft.Bcl
- Microsoft.Bcl.Build
- Microsoft.Net.Http
- Microsoft.ProjectOxford.Common
- Microsoft.ProjectOxford.Face
- Newtonsoft.Json
- OpenCvSharp3-AnyCPU
