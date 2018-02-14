using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Face;

using System.Threading;

using System.Drawing;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.IO;

namespace OpenCVTest01
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        // camera
        private int INTERVAL = 250; // 4 fps
        private int EXPOSURE = 0;
        private bool isCameraRunning = false;
        private int frameCount = 0;
        Timer timer;        
        VideoCapture camera;
        Mat mat;
        Bitmap imgSource, imgGrab;
        List<Mat> imageArray;
        List<int> labelArray;

        // api
        private readonly IFaceServiceClient faceServiceClient =
            new FaceServiceClient("7b39d7c609de4c5a85235df9717ba821", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");

        // faces
        // FisherFaceRecognizer faceRecognizer;
        Mat preview;

        // training
        String personName = "";
        String groupName = "";
        String groupId = "";
        int count = 0;

        // files
        String baseDir = "d:\\cam\\08\\";
        String grabDir = "d:\\cam\\08\\grab\\";
        String grabPath;
        List<String> imagePaths;

        // misc
        SolidColorBrush colorOn = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff, 0x64, 0xdd, 0x17));
        SolidColorBrush colorOff = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff, 0xff, 0xdd, 0xdd));

        public MainWindow()
        {
            InitializeComponent();
            BtnCamera.Background = colorOff;

            // images
            imageArray = new List<Mat>();
            labelArray = new List<int>();
            imagePaths = new List<string>();
            //capturedImages = new List<System.Windows.Controls.Image>();

            // faces
            // faceRecognizer = FisherFaceRecognizer.Create();
        }

        private void _(String l)
        {
            Log.Text = l + "\n" + Log.Text;
        }

        private void BtnCamera_Click(object sender, RoutedEventArgs e)
        {
            if (isCameraRunning)
            {
                // turn it off
                isCameraRunning = false;
                BtnCamera.Background = colorOff;
                CameraOff();
            }
            else
            {
                // turn it on
                isCameraRunning = true;
                BtnCamera.Background = colorOn;
                CameraOn();
            }
        }

        private void CameraOn()
        {
            
            camera = new VideoCapture();
            camera.Open(0);
            camera.Exposure = EXPOSURE;

            timer = new Timer(CameraGrabImage, 0, 0, INTERVAL);
            isCameraRunning = true;
        }

        private void CameraOff()
        {
            this.Dispatcher.Invoke(() =>
            {
                camera.Dispose();
                timer.Dispose();
                isCameraRunning = false;
            });
        }

        private void GeneratePreview(Mat m)
        {
            System.Windows.Controls.Image cap = new System.Windows.Controls.Image();
            cap.Source = m.ToBitmapSource();
            cap.Width = 60;
            cap.Height = 60;
            cap.Margin = new Thickness(0, 0, 5, 5);            
            panelPreviews.Children.Add(cap);
        }
        private void GeneratePreview(String f)
        {

            System.Windows.Controls.Image cap = new System.Windows.Controls.Image();
            cap.Source = new Bitmap(f).ToBitmapSource();
            cap.Width = 60;
            cap.Height = 60;
            cap.Margin = new Thickness(0, 0, 5, 5);
            panelPreviews.Children.Add(cap);
        }

        private void CameraGrabOne(Mat mat)
        {
            this.Dispatcher.Invoke(() =>
            {                
                imageArray.Add(mat);
                _(String.Format("Added {0} to imageArray", imageArray.Count));
                Status.Content = String.Format("{0} images in array", imageArray.Count);

                
            });
        }

        private void GrabFace(Mat mat)
        {
            var srcImage = mat;
            _(String.Format("Loading image..."));
            Cv2.WaitKey(1);

            var grayImage = new Mat();
            // _("Generating greyscale");
            Cv2.CvtColor(srcImage, grayImage, ColorConversionCodes.BGRA2GRAY);
            // _("Creating histogram...");
            Cv2.EqualizeHist(grayImage, grayImage);
            // _("Loading Haar cascade...");
            var cascade = new CascadeClassifier("D:\\cascades\\haarcascade_frontalface_default.xml");
            // var nestedCascade = new CascadeClassifier("D:\\cascades\\haarcascade_eye_tree_eyeglasses.xml");
            // _("Detecting faces...");
            
            var _faces = cascade.DetectMultiScale(
                image: grayImage,
                scaleFactor: 1.1,
                minNeighbors: 3,
                flags: HaarDetectionType.DoCannyPruning | HaarDetectionType.DoRoughSearch | HaarDetectionType.ScaleImage,
                minSize: new OpenCvSharp.Size(30, 30)
            );
            // _(String.Format("Faces detected: {0}", _faces.Length));
            
            if (_faces.Length > 0)
            {
                _("Face found! Extracting...");
                foreach (var faceRect in _faces)
                {
                    var faceBitmap = new Bitmap(mat.ToBitmap());
                    using (var g = Graphics.FromImage(faceBitmap))
                    {
                        g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(200, 0, 0, 0)), g.ClipBounds);

                        var br = new SolidBrush(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightGreen));

                        var box = new System.Drawing.Rectangle(faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height);

                        g.DrawRectangle(Pens.LightGreen, box);

                        int rTop = faceRect.Top + faceRect.Height;

                        g.FillRectangle(br, faceRect.Left, rTop, faceRect.Width, faceRect.Height);

                        ImgPreviewBox.Source = faceBitmap.ToBitmapSource();

                    }



                    count++;
                    var detectedFaceImage = new Mat(srcImage, faceRect);
                    
                    if (groupName != "" && personName != "")
                    {
                        String iPath = String.Format("{0}{1}\\{2}\\{3}.png", baseDir, groupName, personName, count);
                        _(String.Format("Saving {0}...", iPath));
                        imagePaths.Add(iPath);
                        detectedFaceImage.SaveImage(iPath);
                        GeneratePreview(iPath);
                    }
                }
            }
        }

        private void CameraGrabImage(object state)
        {
            if (isCameraRunning)
            {
                
                this.Dispatcher.Invoke(() =>
                {
                    
                    frameCount++;
                    Status.Content = String.Format("Frame #{0} captured every {1} milliseconds ({2} FPS)", frameCount, INTERVAL, 1000/INTERVAL);
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        try
                        {
                            mat = new Mat();
                            camera.Read(mat);
                            imgSource = BitmapConverter.ToBitmap(mat);
                            this.Dispatcher.Invoke(() =>
                            {
                                imgLiveCam.Source = imgSource.ToBitmapSource();
                                if ((bool)CheckBoxRecord.IsChecked)
                                {
                                    CameraGrabOne(mat);
                                }
                                if ((bool)CheckBoxLiveFaces.IsChecked)
                                {
                                    _("Grabbing live face...");
                                    GrabFace(mat);
                                }
                            });
                        }
                        catch (ObjectDisposedException e)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                Status.Content = "Camera closed. " + e.Message;
                            });
                        }
                    }).Start();

                });
            }
            else
            {
                CameraOff();
            }
        }

        private void BtnSingle_Click(object sender, RoutedEventArgs e)
        {
            mat = new Mat();
            camera.Read(mat);
            imgSource = BitmapConverter.ToBitmap(mat);
            imgLiveCam.Source = imgSource.ToBitmapSource();
            CameraGrabOne(mat);
        }

        private void GrabFacesFromStream()
        {
            if (personName == "") { MessageBox.Show("Blank person name"); return; }
            if (groupName == "") { MessageBox.Show("Blank group name"); return; }
            _(String.Format("Scanning {0} images...", imageArray.Count));
            for (int i = 0; i < imageArray.Count(); i++)
            {
                GrabFace(imageArray[i]);
            }
        }

        private void BtnGrabFaces_Click(object sender, RoutedEventArgs e)
        {
            GrabFacesFromStream();
        }

        private void BtnSetName_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPersonName.Text == "") { MessageBox.Show("Blank person name"); return; }
            if (TxtGroupName.Text == "") { MessageBox.Show("Blank group name"); return; }
            personName = TxtPersonName.Text;
            _(String.Format("Person name set to {0}", personName));
            groupName = TxtGroupName.Text;
            _(String.Format("Group name set to {0}", groupName));
            System.IO.Directory.CreateDirectory(String.Format("{0}{1}\\{2}", baseDir, groupName, personName));
            _(String.Format("{0}{1}\\{2} created", baseDir, groupName, personName));
            groupId = groupName.Replace(" ", "").ToLower();
        }

        private void BtnClearFaces_Click(object sender, RoutedEventArgs e)
        {
            count = 0;
            panelPreviews.Children.Clear();
            TxtPersonName.Text = "";
            TxtGroupName.Text = "";
            personName = "";
            groupName = "";
            groupId = "";
            imageArray = new List<Mat>();
            labelArray = new List<int>();
            imagePaths = new List<string>();
            _("Faces cleared.");
        }

        private async void BtnTrain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await faceServiceClient.TrainPersonGroupAsync(groupId);

                TrainingStatus trainingStatus = null;
                while (true)
                {
                    _("Training...");
                    try
                    {
                        trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(groupId);
                        _(String.Format("Training: {0}...", trainingStatus));
                        if (trainingStatus.Status != Microsoft.ProjectOxford.Face.Contract.Status.Running)
                        {
                            break;
                        }

                        await Task.Delay(1000);
                    }
                    catch (FaceAPIException eGetPersonGroupTrainingStatusAsync)
                    {
                        _(eGetPersonGroupTrainingStatusAsync.ErrorMessage);
                    }
                    catch (TaskCanceledException terr)
                    {
                        _(terr.Message);
                    }
                }

                _("Training successfully completed");
            }
            catch (FaceAPIException eTrainPersonGroupAsync)
            {
                _(eTrainPersonGroupAsync.ErrorMessage);
            }

        }

        private async void BtnLoadToTrainer_Click(object sender, RoutedEventArgs e)
        {

            if (groupName == "" || personName == "")
            {
                MessageBox.Show("Blank group or person name");
                return;
            }
            

            _(String.Format("Creating group {0}...", groupId));
            try
            {
                await faceServiceClient.CreatePersonGroupAsync(groupId, groupName);
            }
            catch (FaceAPIException eCreatePersonGroupAsync)
            {
                _(String.Format("{0}", eCreatePersonGroupAsync.ErrorMessage));
            }

            _("Creating person " + personName + "...");
            try
            {
                CreatePersonResult person = await faceServiceClient.CreatePersonAsync(groupId, personName);
                foreach (String imagePath in imagePaths)
                {
                    using (Stream s = File.OpenRead(imagePath))
                    {
                        try
                        {
                            _(String.Format("Processing {0}...", imagePath));
                            await faceServiceClient.AddPersonFaceAsync(groupId, person.PersonId, s);
                        }
                        catch (FaceAPIException eAddPersonFaceAsync)
                        {
                            _(eAddPersonFaceAsync.ErrorMessage);
                        }
                    }
                }
                _("Done.");
            }
            catch(FaceAPIException eCreatePerson)
            {
                _(eCreatePerson.ErrorMessage);
            }

            
        }

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    _(String.Format("Extracting faces from {0}...", imageFilePath));
                    var faces = await faceServiceClient.DetectAsync(imageFileStream,
                        true,
                        true,
                        new FaceAttributeType[] {
                            FaceAttributeType.Gender,
                            FaceAttributeType.Age,
                            FaceAttributeType.Emotion
                        });
                    _(String.Format("{0} faces found!", faces.Length));
                    return faces.ToArray();
                }
            }
            catch (FaceAPIException ex)
            {
                //MessageBox.Show(ex.Message);
                _(ex.ErrorMessage);
                return new Face[0];
            }
        }

        private async void Identify()
        {
            _(String.Format("Attempting to identify face using {0} group...", groupId));
            try
            {
                Face[] faces = await UploadAndDetectFaces(grabPath);
                var faceIds = faces.Select(face => face.FaceId).ToArray();
                _("Calling service...");
                try
                {
                    var results = await faceServiceClient.IdentifyAsync(groupId, faceIds);

                    foreach (var identifyResult in results)
                    {
                        _("Parsing results...");
                        if (identifyResult.Candidates.Length != 0)
                        {
                            _("Candidate found...");
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            try
                            {
                                _("Trying to identify person...");
                                var person = await faceServiceClient.GetPersonAsync(groupId, candidateId);

                                // user identificated: person.name is the associated name
                                _(String.Format("****************** {0} identified ******************", person.Name));
                            }
                            catch (FaceAPIException eGetPersonAsync)
                            {
                                _(eGetPersonAsync.ErrorMessage);
                            }
                            catch (TaskCanceledException terr)
                            {
                                _(terr.Message);
                            }

                        }
                        else
                        {
                            // user not recognized
                            _("Person not recognized.");
                        }
                    }
                }
                catch (FaceAPIException eIdentifyAsync)
                {
                    _(eIdentifyAsync.ErrorMessage);
                }
            }
            catch (FaceAPIException eUploadAndDetectFaces)
            {
                _(eUploadAndDetectFaces.ErrorMessage);
            }
            catch (System.Net.Http.HttpRequestException httper)
            {
                _(httper.Message);
            }
        }

        private void BtnIdentify_Click(object sender, RoutedEventArgs e)
        {
            Identify();
        }

        private void BtnSetCamera_Click(object sender, RoutedEventArgs e)
        {
            if(TxtExposure.Text == "" || TxtFPS.Text == "" )
            {
                MessageBox.Show("Invalid input");
                return;
            }
            int exposure = int.Parse(TxtExposure.Text);
            int interval = 1000 / int.Parse(TxtFPS.Text);

            EXPOSURE = exposure;
            INTERVAL = interval;
        }

        private void BtnDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            _("Deleting " + groupId);
            try
            {
                faceServiceClient.DeletePersonGroupAsync(groupId);
                _(groupId + " deleted.");
            }catch(FaceAPIException ex)
            {
                _(ex.ErrorMessage);
            }
        }

        private void BtnLiveFace_Click(object sender, RoutedEventArgs e)
        {
            var haarCascade = new CascadeClassifier("D:\\cascades\\haarcascade_frontalface_default.xml");

            if (personName == "") { MessageBox.Show("Blank person name"); return; }
            if (groupName == "") { MessageBox.Show("Blank group name"); return; }
            _(String.Format("Scanning {0} images...", imageArray.Count));
            for (int i = 0; i < imageArray.Count(); i++)
            {
                GrabFace(imageArray[i]);
            }

        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            
            if (isCameraRunning)
            {
                imgGrab = imgSource;
                _("Grabbing image...");
                ImgCapture.Source = imgGrab.ToBitmapSource();
                Directory.CreateDirectory(grabDir);
                grabPath = grabDir + "\\grabbed.png";
                _("Saving to " + grabPath + "...");
                imgGrab.Save(grabPath);
                _("Saved.");
            }
            else
            {
                MessageBox.Show("Turn on camera");
                return;
            }
        }
    }
}
