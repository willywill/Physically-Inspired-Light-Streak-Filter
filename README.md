### Physically-Inspired Light Streak Filter


![Light Streaks](https://i.imgur.com/Ww8H2lx.jpg)


This effect is based on the process of bright light scattering through the lens elements of a camera. The light streaks are generated at small apertures (lens opening) where the number/shape of streaks depends on the aperture blades and aperture blade shape in the lens respectively.

---

### Getting Started

> **NOTE:**
It is highly recommended that you use this with HDR, a Tonemapper of your liking, and Linear Color Space.

Simply place `LightStreakFilter.cs` onto your camera, configure the settings as shown below as you wish.

#### Settings
------

| Setting       | Description   |
| ------------- |-------------|
| Quality      | Self-explanatory |
| Streaks      | Number of streaks rendered      |
| Power | Exponential strength of the effect      |
| Strength      | Intensity of the effect |
| Threshold      | Selectively choose which pixels render streaks based on brightness      |
| Attenuation | How quickly to fade away streak intensity from the bright pixel      |
| Angle      | Rotation angle of the streak filter |
| Diffraction      | How much light bends from lens source      |
| Rotate based on camera | Camera rotations cause the angle to change?  |
| Spectrum Texture      | Diffraction modulation texture |

------

#### Further Reading

- Frame Buffer Postprocessing Effects in DOUBLE-S.T.E.A.L
- [Real-Time Dynamic Simulation of the Scattering in the Human Eye](https://people.mpi-inf.mpg.de/~ritschel/Papers/TemporalGlare.pdf)
- [Physically-Based Glare Effects for Digital Images](http://luthuli.cs.uiuc.edu/~daf/courses/rendering/papers3/spencer95.pdf)
- [Real-time Rendering of High Quality Glare Images](https://www.scitepress.org/papers/2006/13548/13548.pdf)
- [Glare Generation Based on Wave Optics](https://pdfs.semanticscholar.org/f3cb/326ea861899cbbc4197078360c5e076fd1e5.pdf)
- [Real-Time 3D Scene Post-Processing](http://developer.amd.com/wordpress/media/2012/10/Oat-ScenePostprocessing.pdf)

------

*This code was adapated from and based on the implementation [here.](https://github.com/nobnak/KawaseLightStreakUnity)*
  
