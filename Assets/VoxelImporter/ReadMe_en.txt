-----------------------------------------------------
	Voxel Importer
	Copyright (c) 2016 AloneSoft
	http://alonesoft.sakura.ne.jp/
	mail: support@alonesoft.sakura.ne.jp
	twitter: @AlSoSupport
-----------------------------------------------------

Thank you for purchasing "Voxel Importer".


[How to Update] 
Remove old VoxelImporter folder
Import VoxelImporter


[Documentation]
Assets/VoxelImporter/Documents


[Tutorial]
Humanoid Setup Tutorial
https://youtu.be/hkudVsxtxn4
Frame Animation Tutorial
https://youtu.be/rg6KhqDq-bU
Voxel Chunks Object
https://youtu.be/9Fh5WRbrIGE


[Examples]
Assets/VoxelImporter/Examples


[Support]
mail: support@alonesoft.sakura.ne.jp
twitter: @AlSoSupport


[Update History]
Version 1.1.3p2
- FIX : Unity2018.1 support
- FIX : Problems hard to see with Dark Skin

Version 1.1.3p1
- FIX : Unity2018.1 : Fixed 'Configure Avatar' error

Version 1.1.3
- ADD : MagicaVoxel 0.99.1 World Editor (multi object) support
- ADD : VoxelChunksObject : Added "Refresh all chunks" button to update completely
- FIX : ColladaExporter : Error handling correction
- FIX : VoxelChunksObject : Disable Refresh operation with Prefab selection
- FIX : Import error for non-lower case extension

Version 1.1.2p1
- FIX : Explosion : Supports not to split Mesh when number of vertices is 65k or more (Only Unity 2017.3 or later)
- FIX : DaeExporter : Correcting errors in the material without the '_Color' property

Version 1.1.2
- ADD : ScriptedImporter
- ADD : BonePosition : Scaling All
- ADD : BonePosition : Change rotation setting possible
- ADD : VoxelStructure
- ADD : Optimize : Combine faces
- ADD : Examples : EditMesh, CreateCubes
- FIX : BonePosition : The position returns when changing the position after setting the Rig type to Humanoid

Version 1.1.1p3
- FIX : Fixed an issue that can not be imported when using .Net 4.6

Version 1.1.1p2
- FIX : Explosion : Fixed an issue where Settings contents were not reflected at run time

Version 1.1.1p1
- FIX : Fixed bug that palette and material can not be read correctly by reading vox saved on MagicaVoxel 0.99 alpha
		Supports only single object data, not reading multiple object data

Version 1.1.1
- ADD : Supports more than 65k vertices (Only Unity 2017.3 or later)
- FIX : Simple mode

Version 1.1.0
- ADD : Simple mode
- ADD : Voxel Skinned Animation Object : Support for adding Animator and Animation components automatically by changing Animation Type
- FIX : HiDPI / Macbook Retina screen resolution support - Problem of misalignment of brush

Version 1.0.9p6
- FIX : ColladaExporter : Animation and Specular color
- FIX : Edit Bone Animation : Mirror side is not recorded in animation

Version 1.0.9p5
- FIX : ColladaExporter : Animation and Material Color
- FIX : Gizmos : Fixed problem that Selection Wire disappeared

Version 1.0.9p4
- FIX : ColladaExporter

Version 1.0.9p3
- FIX : ColladaExporter
- FIX : Skeleton : Preview

Version 1.0.9p2
- FIX : WeightPaint : Undo
- FIX : ColladaExporter : Animation Export

Version 1.0.9p1
- FIX : ColladaExporter : Change FootIK valid at the time of the warning correspondence, FootIK effective to default.
- FIX : Skeleton : Change Preview

Version 1.0.9
- ADD : ColladaExporter : Animation support
- ADD : Unity5.6 or later : Mecanim Humanoid Upper Chest support
- FIX : ColladaExporter : Fixed problem that Hierarchy of file output from 'VoxelSkinnedObject' changed from before output
- FIX : Change operating environment to Unity 5.4.0 or later

Version 1.0.8p3
- FIX : FrameAnimation : Fixed bug in reading additional files outside the project

Version 1.0.8p2
- ADD : Reflect part of MagicaVoxel's material settings

Version 1.0.8p1
- FIX : Added countermeasure for problem that polygon crack is visible at low resolution

Version 1.0.8
- ADD : Collada Exporter
- FIX : Change Voxel Data load timing
- FIX : Import Offset scaling

Version 1.0.7p1
- FIX : Generate Low-Poly Texture
- FIX : Edit Bone Animation - Move
- FIX : Unity 5.6.0 support
- FIX : Explosion : Shader : UNITY_MATRIX_MVP -> UnityObjectToClipPos
 
Version 1.0.7
- ADD : Editor : Window
- ADD : Prefab creation simplification
- FIX : Import Flag
- FIX : Explosion Material
- FIX : Unity5.5 support

Version 1.0.6.p3
- FIX : Manual

Version 1.0.6.p2
- ADD : Bone - "Remove This Bone"
- FIX : HiDPI / Macbook Retina screen resolution support - Problem of position shift

Version 1.0.6.p1
- ADD : ContextMenu - "Remove All Voxel Importer Compornent"
- FIX : Materials - "Load From Voxel File"
- FIX : Voxel Skinned Animation Object - Erase MeshFilter
- FIX : Problem in the case of the Bone of the children of Voxel Skinned Animation Object there is a Voxel Skinned Animation Object
- FIX : Import Mode - Low Poly - Texture generation

Version 1.0.6
- ADD : MagicaVoxel 0.98 support - vox animes and materials
- ADD : Optimize - "Ignore Cavity"
- ADD : Import Offset - Set
- FIX : Frame Animation List Window

Version 1.0.5
- ADD : Voxel Frame Animation
- ADD : Bone Position Editor - "Snap to half-voxel"
- ADD : Editor help
- FIX : Bone weight mirroring

Version 1.0.4.p2
- ADD : Weight preview of the root bone
- ADD : Weight preview transparent display
- ADD : File open in the Drag & Drop
- ADD : Update component flags
- ADD : Context Menu "Save All Unsaved Assets" & "Reset All Assets"
- FIX : Undo

Version 1.0.4.p1
- ADD : Add and remove the root bone
- FIX : Error check

Version 1.0.4
- ADD : Voxel Explosion

Version 1.0.3.p2
- FIX : Prefab Edit
- FIX : Undo
- FIX : Create Avatar

Version 1.0.3.p1
- FIX : Prefab Edit
- FIX : Undo

Version 1.0.3
- ADD : Multi Material
- FIX : Mirroring

Version 1.021
- ADD : Configure Humanoid Avatar
- ADD : Folding corresponding menu

Version 1.02
- ADD : Mecanim Humanoid support
- ADD : Edit Weight Paint : Auto Normalize
- FIX : "Disable Animation" and "Mirror Animation"
- FIX : Modify so that there are no unintended change of "Bone Position"

Version 1.011
- FIX : Weight mirroring process
- FIX : Undo

Version 1.01
- ADD : "Voxel Chunks Object"
- FIX : Weight mirroring process
- FIX : Undo
- FIX : "BoneTemplate" Search
- FIX : Delete shortcut key competing

Version 1.00
- first release

