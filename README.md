# sl-unity-tasks

A C# Unity project that provides assets to create and execute Virtual Reality (VR) tasks used to facilitate
experiments in the Sun (NeuroAI) lab.

[![C#](https://tinyurl.com/bdd689s9)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Unity](https://img.shields.io/badge/Unity-6000.3.3f1_LTS-000000?logo=unity&logoColor=white)](https://unity.com/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

___

## Detailed Description

This project provides assets and bindings for building Virtual Reality (VR) tasks used by some data acquisition systems
in the Sun lab to conduct experiments. Primarily, the project is designed to construct an **infinite linear corridor**
environment and display it to the animal during runtime using a set of three Virtual Reality monitors (screens).

This project is specialized to work with the main [sl-experiment](https://github.com/Sun-Lab-NBB/sl-experiment) library
used by all Sun lab data acquisition systems. It uses [MQTT](https://mqtt.org/) to bidirectionally communicate with the
sl-experiment runtimes and relies on sl-experiment to provide it with the data on animal's behavior during the VR task
execution.

This project extends the original [GIMBL](https://github.com/winnubstj/Gimbl) repository, refactored to improve
flexibility for creating Unity VR tasks. It provides an interface for building and modifying tasks using prefabricated
assets ('prefabs'), deprecates GIMBL functionality now handled by sl-experiment (logging, unused MQTT topics), and
removes legacy technical debt.

___

## Features

- Runs on Windows, Linux, and macOS.
- Supports tasks with multiple corridor segments and probabilistic transitions between them.
- Includes agentic coding support with Claude Code skills for codebase exploration and style guide compliance.
- Provides automated task structure verification to validate prefab positions against YAML template constants.
- GPL 3 License.

___

## Table of Contents

- [Dependencies](#dependencies)
- [Installation](#installation)
- [Usage](#usage)
- [Creating New Tasks](#creating-new-tasks)
- [Developer Notes](#developer-notes)
- [Versioning](#versioning)
- [Authors](#authors)
- [License](#license)
- [Acknowledgements](#acknowledgments)

___

## Dependencies

### Internal Dependencies

These dependencies are automatically installed with the project either as .dll files or as asset collections:

- [M2MQTT package](https://github.com/eclipse/paho.mqtt.m2mqtt) version **4.3.0** (bundled with gimbl).
- [SharpDX package](https://github.com/sharpdx/SharpDX/tree/master) version **4.2.0** (bundled with gimbl).

### External Dependencies

The user must install these dependencies before working with this Unity project:

- [MQTT broker](https://mosquitto.org/) version **2.0.21**. This project was tested with the broker running locally,
  using the **default** IP (127.0.0.1) and Port (1883) configuration.
- [Unity Game Engine](https://unity.com/products/unity-engine) version **6000.3.3f1 LTS**.
- [Blender](https://www.blender.org/download/) version **4.5.0 LTS**.

___

## Installation

### Source

1. Install the [Unity hub](https://unity.com/download) and use it to install the required Unity Game Engine version.
2. Download this repository to a local machine using a preferred method, such as Git-cloning. Use one of the stable
   releases from [GitHub](https://github.com/Sun-Lab-NBB/sl-unity-tasks/releases).
3. From the Unity Hub, select `add project from disk` and navigate to the local folder containing the downloaded
   repository: <br> <img src="imgs/AddProjectFromDisk.png" width="300"/>

**Hint.** If the correct Unity version is not installed when the project is imported, the Unity Hub displays a warning
next to the project name. Click on the warning and install the recommended Unity version:
<br> <img src="imgs/InstallRecommendedVersion.png" width="600"/>

___

## Usage

This section discusses how to use existing tasks to conduct experiments and create new tasks using the project.
**Note!** This library is specifically written to work with the
[sl-experiment](https://github.com/Sun-Lab-NBB/sl-experiment) library and will likely not work in other contexts
without modification.

### Creating New Tasks

The key feature of this project is the **task creator**: a system for quickly making any infinite corridor task with or
without probabilistic transitions between corridor segments.

#### Task Definition

Each **task** can be conceptualized as a set of infinite corridor **segments** and the **transition probabilities**
between them. Each segment is split into **cues**, which are portions of the corridor walls that have different
colors/textures. Since each task segment typically contains a stimulus trigger zone that conditionally delivers stimuli
to the animal (water rewards, air puffs, etc.), traversing each segment typically constitutes a single **experiment
trial**. Therefore, the sequence of wall cues that makes each segment is referred to as the **cue sequence** in task
configuration files.

Overall, a set of segments can represent any task graph depicting transitions between infinite corridor cues. For
example, the cue graph below can be represented by two segments with uniform transition probabilities between
each other:

<!--suppress CheckImageSize -->
<img src="imgs/cue_graph.png" width="233" alt="graph picture">

1. **Segment 1**: A, B, C
2. **Segment 2**: A, B, D, C

During experiment, both segments are typically reused many times to create a long sequence of segments to be experienced
by the animal during runtime.

In addition to the general task structure, there are additional parameters to be considered for each task, including:

- The length of each cue region.
- The length of non-cue ('gray') wall regions between the cue regions.
- The graphical texture (pattern) of each wall cue.
- The graphical texture of non-cue wall regions (usually gray color, hence the name **gray regions**).
- The graphical texture of the corridor floor.
- The stimulus trigger zone locations and the conditions for the animal to receive the stimulus.

#### Implementation

To create a task according to the desired specification, two assets need to be generated: a Unity prefab for each
segment and a YAML configuration file. The easiest way to create these assets is to start with an already existing
task and modify it to match the desired parameters. Use ctrl/cmd D to duplicate existing segment prefabs and copy
existing `.yaml` files from `Assets/InfiniteCorridorTask/Configurations/`.

#### Segment Prefabs

All segment prefabs must be placed in the directory **Assets/InfiniteCorridorTask/Prefabs**. Double-clicking on a prefab
opens up Unity's prefab editor. **Hint!** To verify that the file being edited is a prefab and not a GameObject, ensure
that the scene has a **blue** background.

<img src="imgs/segment_prefab.png" width="600" alt="">

Each prefab includes several key elements:

- **Stimulus Trigger Zone**: The parent zone that manages stimulus delivery. Its behavior depends on which child zone
  is present (Guidance Zone or Occupancy Zone).
- **Guidance Zone**: A child of the Stimulus Trigger Zone used in lick mode trials.
- **Reset Zone**: After successfully triggering a stimulus delivery, the animal must pass through this zone before
  another stimulus can be triggered.
- **Occupancy Zone** *(optional)*: A child of the Stimulus Trigger Zone used in occupancy mode trials.

**Zone Behavior Modes:**

The Stimulus Trigger Zone operates in one of two modes based on which child zone is present. The trigger mode
determines *how* a stimulus is delivered, not *what* stimulus is delivered. Any stimulus type (water, air puff, etc.)
can be paired with either trigger mode.

1. **Lick Mode** (with Guidance Zone child):
   - When **Require Lick** is enabled: The animal must lick within the Stimulus Trigger Zone to receive the stimulus.
   - When **Require Lick** is disabled (guidance mode): The stimulus is delivered automatically when the animal reaches
     the Guidance Zone. The animal can still lick anywhere in the Stimulus Trigger Zone to receive the stimulus early.

2. **Occupancy Mode** (with Occupancy Zone child):
   - The animal must remain in the Occupancy Zone for the required duration to **disarm** the trigger zone's boundary.
   - If the animal leaves early and collides with the boundary while it is still **armed**, the stimulus is delivered.
   - When **Require Wait** is disabled (guidance mode): The library sends an MQTT message requesting the treadmill
     brakes to lock, enforcing the occupancy requirement by preventing the animal from leaving early.

**Note:** The Sun Lab currently uses lick mode for reward delivery (water) and occupancy mode for aversion stimuli
(air puff), but this pairing is a convention rather than a technical requirement. Future experiments may use different
stimulus-trigger combinations.

Once each prefab segment is created, an additional prefab must be made for padding. This padding prefab should be a long
empty corridor, and it is used during task runtime to give the animal an illusion that the corridor is infinite.

#### YAML Configuration File

The **task configuration file** ties the segment prefabs together and is required for creating and running tasks. These
files are stored in `Assets/InfiniteCorridorTask/Configurations/` with a `.yaml` extension.

**Note:** The configuration schema is derived from the
[sl-shared-assets](https://github.com/Sun-Lab-NBB/sl-shared-assets) library and always matches the current state of
that library's data classes. See `task_template_data.py` in sl-shared-assets for the authoritative schema definition.

**File Naming Convention:**

Template files follow the pattern `ProjectAbbreviation_TaskDescription.yaml`:

| Abbreviation | Project Name      |
|--------------|-------------------|
| MF           | MaalstroomicFlow  |
| SSO          | StateSpaceOdyssey |

- Use `_Base` suffix for single-segment training configurations (e.g., `SSO_Shared_Base.yaml`).
- Capitalize each word in the task description.
- The template name and Unity scene name are derived from the filename (without `.yaml`).

**Header Format:**

Each template file must begin with a YAML comment header containing four fields:

```yaml
# Project: [Full project name]
# Purpose: [Single sentence describing the task structure]
# Layout:  [Segment names with cue letters and zone placements]
# Related: [Related template file (parenthetical explanation)]
```

For multi-line fields, align continuation text with the first character after the field name:

```yaml
# Layout:  Segment ABC with the rewarding stimulus (water) trigger zone in cue C.
#          Segment ABDC with the rewarding stimulus (water) trigger zone in cue C.
```

**Schema:**

The structure is:

- **cue_offset_cm** *(number)*: The offset in centimeters from the reset zone to the first cue.

- **cues** *(array\<Cue>)*: The list of all cues used by any segment.
    - **Cue**
        - **name** *(string)*: The unique human-readable label for the cue (e.g., `"A"`, `"Gray"`).
        - **code** *(integer, 0-255)*: The unique integer code for the cue used in logging.
        - **length_cm** *(number)*: The length of the cue in centimeters.

- **segments** *(array\<Segment>)*: The list of all segments.
    - **Segment**
        - **name** *(string)*: The name of the segment prefab (e.g., `"Segment_abc_40cm"`). Must match the prefab file
          name in **Assets/InfiniteCorridorTask/Prefabs**.
        - **cue_sequence** *(string[])*: The ordered list of cues in the segment.
        - **transition_probabilities** *(number[])*: The probabilities of transitioning to each segment. Must sum to 1.
          Optional; if unspecified, uniform transitions are assumed.

- **vr_environment** *(object)*: VR corridor configuration.
    - **corridor_spacing_cm** *(number)*: Distance between consecutive corridors in centimeters.
    - **segments_per_corridor** *(integer)*: Number of segments per corridor. Setting this to 3 is generally enough to
      give the illusion of an infinite corridor.
    - **padding_prefab_name** *(string)*: The name of the padding prefab (usually `"Padding"`).
    - **cm_per_unity_unit** *(number)*: Conversion factor from centimeters to Unity units.

- **trial_structures** *(dict\<string, TrialStructure>)*: Maps trial names to their spatial configurations.
    - **TrialStructure**
        - **segment_name** *(string)*: The segment this trial uses.
        - **stimulus_trigger_zone_start_cm** *(number)*: Start of the stimulus trigger zone in centimeters.
        - **stimulus_trigger_zone_end_cm** *(number)*: End of the stimulus trigger zone in centimeters.
        - **stimulus_location_cm** *(number)*: Position of the stimulus boundary in centimeters.
        - **show_stimulus_collision_boundary** *(boolean)*: Determines whether to show the stimulus boundary to the
          animal.
        - **trigger_type** *(string)*: The trigger mode for the zone. Must be `"lick"` for segments with a
          Guidance Zone child or `"occupancy"` for segments with an Occupancy Zone child. This field specifies the
          trigger mechanism, not the stimulus type.

See existing configuration files in `Assets/InfiniteCorridorTask/Configurations/` for examples.

**Important:** This project includes an agentic skill (`/verifying-task-templates`) that validates configuration
template files against existing prefabs. Developers and AI agents are highly encouraged to use this skill when creating
or modifying configuration files to ensure zone positions, segment lengths, and other spatial parameters match the
actual prefab state.

#### 'CreateTask' Tab

Once the YAML configuration file is created, use the **CreateTask → New Task** command. This will open a file window to
select the configuration file. Once the file is selected, a secondary prompt will open to name and save the prefab.
Once created, the prefab can be loaded and executed as any pre-created task that comes with the project (see below).

<img src="imgs/createTask.png" width="700" alt="">

### Loading Existing Tasks

Each distribution of the project contains all tasks currently used in the Sun lab. To use an existing task, open the
Unity project and follow these steps:

1. Create a new scene by clicking File → New Scene. Instead of using the default scene template, select
   **ExperimentTemplate** as the template. **Note!** The first time this Unity project opens, it uses an empty scene.
   If prompted, do ***not*** save this empty scene.
   <br> <img src="imgs/newScene.png" width="600">
2. Navigate to **Assets/InfiniteCorridorTask/Tasks**. This folder contains prefabricated Unity assets (prefabs) for
   all tasks actively or formerly used to conduct experiments in the Sun lab. Drag the prefab for the desired task into
   the hierarchy window and wait for it to be loaded into the scene. **Note!** If Preferences > Scene View > 3D
   Placement Mode is set to "World Origin," then dragging the prefab into the hierarchy window will automatically
   position the task correctly.
   <br> <img src="imgs/hierarchy_window.png" width="800">
3. Select the task's **GameObject** in the **Hierarchy** window and view the **Inspector** window. The **Inspector**
   window reveals the **Transform** component and the **Task** script. There are two things that must be verified at
   this point:
    1. That the transform's position is set to (0, 0, 0).
    2. That the **Actor** parameter is set. If it is None, use the dropdown menu to set it to the **Actor Object** in
       the scene.
4. The *Task* script contains additional parameters which should not need to be modified:
    - **Require Lick**: Determines whether the animal must lick within the stimulus trigger zone to receive the
      stimulus. If disabled (guidance mode), the stimulus is delivered automatically when the animal reaches the
      guidance zone. **Note!** During sl-experiment runtimes, this parameter is automatically overridden by the
      sl-experiment GUI and runtime logic.
    - **Require Wait**: Determines whether the animal must remain in the occupancy zone for the required duration to
      disarm the trigger zone's start boundary. If disabled (guidance mode), the library sends an MQTT message
      requesting the treadmill brakes to lock, enforcing the occupancy requirement. **Note!** During sl-experiment
      runtimes, this parameter is automatically overridden by the sl-experiment GUI and runtime logic.
    - **Track Length**: The length of the track's wall cue sequence, in Unity units, to pre-create before runtime. This
      is most relevant for tasks with multiple segments and random transitions between them. Pre-creating the cue
      sequence before runtime allows sl-experiment to accurately track transitions between trials and support
      trial-specific logic while treating the experiment runtime as a monolithic sequence of trials. **Note!** If the
      animal traverses the entire pregenerated track, the Unity task starts making on the fly decisions about which
      segment the animal enters at the end of each trial. Likely, this will cause sl-experiment to abort with an error,
      as it is not notified of these additional trials. Therefore, **it is advised to pre-generate a long cue sequence
      at each runtime, guaranteeing the animal is not able to fully traverse it at runtime**.
    - **Track seed**: The seed to use for resolving random transitions between segments. This is helpful when running
      many experiments with the exact same pattern of segment transitions. If set to -1, then no seed is used and
      transitions are randomized at each task runtime.
    - **Config Path**: The file path to the YAML configuration file associated with the task. **Note!** If the
      configuration file specified by this parameter is no longer found at the target path, the game becomes
      non-functional. To fix this, change this parameter to specify the correct path (relative to the local root) or
      recreate the task. See the ['creating new tasks'](#creating-new-tasks) section for more details about this file.
5. Select File > Save As to save the scene in *Assets/Scenes*.
6. Select the **DisplaysWindow** tab located to the right of the Inspector tab. If the tab is not present, reopen it
   by selecting Window > Gimbl. Press `Refresh Monitor Positions`. This reveals a list of the monitors connected
   to the computer. Assign **Camera: LeftMonitor**, **Camera: RightMonitor**, and **Camera: CenterMonitor** to the
   corresponding monitors used for display to the animal. To verify that the monitors were assigned correctly, press
   `Show Full-Screen Views`. For more information about configuring displays, consult the
   [original GIMBL repository](https://github.com/winnubstj/Gimbl?tab=readme-ov-file#setting-up-the-actor).
   **Warning!** Since rebooting the system frequently changes the Monitor output ports, always verify
   monitor assignments before running experiment tasks.
   <br> <img src="imgs/display_tab.png" width="300">
7. Press the play button to run the VR task. Verify that there are no errors displayed in the console window after
   starting (playing) the task. **Hint!** If errors appear, start debugging by examining the **first** error
   printed, which is likely the true error. Subsequent errors are likely a result of running a broken game loop after
   the initial error. **Note!** The template environment is designed for experiments, where motion and licks should be
   sent over the MQTT protocol. To test the task manually, replace the *linear controller* with a *simulated linear
   controller*. Consult [Setting Up the Actor](https://github.com/winnubstj/Gimbl?tab=readme-ov-file#setting-up-the-actor)
   for instructions on this process.

___

## Developer Notes

These notes are primarily directed to project developers and task creators.

* Be careful about modifying segment prefabs. Even after task creation, the task prefab relies on the existence of the
  segment prefabs to run as expected. This means that if segment prefabs are modified later, it will also modify all
  tasks using that prefab. To make small changes to many tasks, use the same segment prefab multiple times to
  automatically synchronize the changes across all modified tasks. To modify one task without changing other tasks
  that use the same prefab, make a new prefab that is a duplicate of the old one and modify the YAML configuration files
  accordingly.
* Most changes to the task structure can be implemented by modifying the segment prefabs. However, modifying a prefab
  may invalidate all specification files using that prefab. The specification file contains a lot of information that
  needs to match the exact state of each prefab, so it is a good practice to ensure the validity of all specification
  files after modifying the prefab. Also, it is good practice to recreate the task from the specification file following
  prefab modification. If the newly created task uses the same name as the old task, it will replace the old task
  prefab.
* The [Loading Existing Tasks](#loading-existing-tasks) section explains how to create a scene to hold the desired task.
  When running multiple experiments (using different tasks) from the same computer, it may be cumbersome to maintain
  multiple Unity projects or to have one Unity project and switch the active task between experiments (within the same
  scene). The best practice is to create a separate scene for each experiment as part of the same Unity project and
  switch between scenes by double-clicking on them. When starting a new experiment, open the desired scene and run the
  task. **Note!** The display configurations are scene-specific, so displays must be reconfigured separately for each
  scene.
* Be cautious when pushing and pulling code with GitHub. Merging branch conflicts is challenging with Unity and will
  likely require changing one of the conflicting branches completely. Try to avoid merge conflicts and focus on making
  changes to assets (prefabs) while avoiding making large changes to the scene. Additionally, it is a good practice to
  close the Unity project before pushing/pulling.
* The original GIMBL package was designed to log all non-brain-activity experiment data. Since this project is
  explicitly designed to work with sl-experiment that now does all logging, **all Unity logging has been removed from
  this project**.
* For information on how to send MQTT messages to Unity, see
  [here](https://github.com/winnubstj/Gimbl/wiki/Example-code-of-MQTT-subscribing-and-publishing).

* Additional cues can be found [here](https://github.com/sprustonlab/vr-visual-cues). To use a new cue:
    1. Convert an `.ai` file to `.png`.
    2. Import the `.png` into Unity as an asset. Place the asset in the Assets/InfiniteCorridorTask/Textures folder.
    3. Create a new material or just duplicate one of the existing materials. Currently, all cues are prefab variants of
       cue A. To keep this structure, duplicate any other material (e.g. CueB). Materials are saved in the
       Assets/InfiniteCorridorTask/Materials folder.
    4. Set the material's texture to the new `.png`.
    5. On the segment being modified, use the Mesh Renderer component to select the new material.

___

## Versioning

This project uses [Semantic Versioning](https://semver.org/). For available versions, see the
[tags on this repository](https://github.com/Sun-Lab-NBB/sl-unity-tasks/tags).

___

## Authors

- Jacob Groner ([Jgroner11](https://github.com/Jgroner11))
- Ivan Kondratyev ([Inkaros](https://github.com/Inkaros))

___

## License

This project is licensed under the GPL3 License: see the [LICENSE](LICENSE) file for details.

___

## Acknowledgments

- All Sun Lab [members](https://neuroai.github.io/sunlab/people) for providing the inspiration and comments during the
  development of this library.
- The creators of the original [GIMBL](https://github.com/winnubstj/Gimbl) package and all dependencies used by that
  package.
