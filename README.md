# Baxter AR Teloperation Application
#### HoloLens 2 augmented reality application to remotely teleop a Baxter dual arm robot.

### Branch Descriptions
- ```main``` - stable build
- ```devel``` - development branch. Working daily on this project, devel is not garenteed to build.


## Getting Started
### Tested Versions
- [Unity 2020.3.35f1](https://unity3d.com/get-unity/download 'https://unity3d.com/get-unity/download')
- [Visual Studio Community 2019 v16.8.3](https://visualstudio.microsoft.com/downloads/ 'https://visualstudio.microsoft.com/downloads/')
- [Windows 10 SDK v10.0.19041.0](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/ 'https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/')

### Opening Project
1. Clone this repo 
    - HTTP: ```git clone --recursive https://github.com/frank-Regal/baxter_ar_teleop.git```
    - SSH: ```git clone --recursive git@github.com:frank-Regal/baxter_ar_teleop.git```
2. Open Unity Hub and click "Add project from disk" from the drop down "Open" menu to find and select your cloned repo.
3. Select 2020.3.35f1 from the drop down menu in the editor version column.
4. Double click to open the project.
5. All third person view work is done in the main scene called ```MainScene``` which should be highlighted in the ***Hierachy panel***.
    - If not open it by navigating and selecting the ```MainScene.unity``` file from within Unity by clicking ```File -> Open Scene -> Scenes -> MainScene.unity```

### Building & Deploying Project
1. From the Unity ***Hierarchy panel***, select the ```RosConnector``` game object.
2. With ```RosConnector``` still selected, navigate to the ```Ros Connector (Script)``` in the ***Inspector panel***.
3. Under the *Serializer* dropdown menu ensure ```Newtonsoft_JSON``` is selected.
4. Under the *Protocol* dropdown menu ensure ```Web Socket UWP``` is selected.
5. Follow this tutorial to build for HoloLens 2: [Build and Deploy to the HoloLens](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/build-and-deploy-to-hololens 'https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/build-and-deploy-to-hololens')
6. Follow this tutorial to deploy to HoloLens 2: [Using Visual Studio to deploy and debug](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2 'https://docs.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2')

## Learn How To Create This Project
Tutorial on how to replicate this project on your own. Please request access if you are restricted access.

- [Tutorial: Building Baxter AR Teloperation Application](https://docs.google.com/document/d/1IbUh4coWxempv4kRiAWMmxlxtNeg4E3t1pIcTP4Cmf0/edit?usp=sharing 'Building Basic Baxter AR Application')

## References
- ROS Sharp plugin used to work with HoloLens 2: [GitHub - EricVoll/ros-sharp](https://github.com/EricVoll/ros-sharp 'https://github.com/EricVoll/ros-sharp')
- Microsoft Mixed Realtiy Toolkit (MRTK): [MRTK Documentation](https://docs.microsoft.com/en-us/learn/modules/learn-mrtk-tutorials/ 'Mixed Reality Toolkit')
