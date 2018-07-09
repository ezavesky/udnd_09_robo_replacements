# Robo Replacements

This project is part of [Udacity](https://www.udacity.com "Udacity - Be in demand")'s [VR Developer Nanodegree](https://www.udacity.com/course/vr-developer-nanodegree--nd017).

## Design and Gameplay
Extensive design, testing, and scope tracking is documented in [design docs](docs/design.pdf).  

### User Feedback
User feedback was sampled both for individual design elements as well as longer play sessions.  Check out the design document for more verbose logging and discussion.

### Future Revisions
TBD

## Rubric Checks
* **Animation (100)** - animation was incorporated for different robot movements; while the quality and diversity will be imroved, a starter for each station is in place.
* **Lighting (100)** - where inuffficient with ambient lighting, real-time lamps were replicated and altered for hue; additioanlly, harnessing the directionality of the skybox a strong ambient light was created that has intersting shadow effects through windows.
* **Locomotion (100)** - locomotion uses a combination of open teleporting along a main path through the output with specific regions of interest denoted with differently colored waypoints
* **video (max 100)** - primarily used to motivate each task, background videos (open source) were opportunistically included
* **gamification (250)**
* **Diegetic UI (250)** - where possible visuals and audio-based communication is given to the user; some text is still required, but attempts were made to condense them into short buttons
* **storyline (250)** - a lightweight dialog manager was hand-created with region triggers to start and end conversation with robots at different tasks; different conversations can be triggered for the same point using a coarse probabilty + random number selection
* **AI (250)** - a system for training and evaluating placement of blocks to guess visual patterns was implemented; a supplemental 2D learner application was created to help create training data for online models in the game
* **speech recognition (500)**
* **user testing (250)** - user testing was accomplished in two ways, early and quick user testing of individual ideas and visual concepts during design and implementation; a secondary, more complete test was also performed when siginificant milestones (like the completion of a task station); notes were taken along the way in the primary design document


## Walkthrough
Snapshots of the overall and individual levels are 
included below.

## Data Sources
Some assets were used from the open source community and are
documented below.

* [Asteroids package](https://assetstore.unity.com/packages/3d/props/asteroid-pack-by-pixel-make-83951)
* [Sci-Fi Environment](https://assetstore.unity.com/packages/3d/environments/sci-fi/sci-fi-styled-modular-pack-82913)
* [toon robot](https://assetstore.unity.com/packages/3d/characters/robots/sleek-toon-bot-free-34490)
    * [sitting and talking animations](https://assetstore.unity.com/packages/3d/animations/everyday-motion-pack-free-115067)
    * [simple animations](https://assetstore.unity.com/packages/templates/systems/ragdoll-and-transition-to-mecanim-38568)
* [space skybox](https://assetstore.unity.com/packages/2d/textures-materials/sky/skybox-volume-2-nebula-3392)
* [howto cooking video](https://www.flickr.com/photos/chegs/with/4743052401/)
* [comic panels font](https://www.dafont.com/comic-panels.font)
* [LeanTween package](https://assetstore.unity.com/packages/tools/animation/leantween-3595)

### Versions
- Unity 2017.2.0f3

