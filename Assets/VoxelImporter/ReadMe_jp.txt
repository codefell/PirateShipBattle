-----------------------------------------------------
	Voxel Importer
	Copyright (c) 2016 AloneSoft
	http://alonesoft.sakura.ne.jp/
	mail: support@alonesoft.sakura.ne.jp
	twitter: @AlSoSupport
-----------------------------------------------------

"Voxel Importer"をご購入いただきありがとうございます。


【更新方法】
・Assets/VoxelImporterを削除
・再度VoxelImporterをインポート


【ドキュメント】
Assets/VoxelImporter/Documents


【チュートリアル】
Humanoid Setup Tutorial
https://youtu.be/hkudVsxtxn4
Frame Animation Tutorial
https://youtu.be/rg6KhqDq-bU
Voxel Chunks Object
https://youtu.be/9Fh5WRbrIGE


【サンプル】
Assets/VoxelImporter/Examples


【サポート】
mail: support@alonesoft.sakura.ne.jp
twitter: @AlSoSupport


【更新履歴】
Version 1.1.3p2
- FIX : Unity2018.1サポート
- FIX : DarkSkinで見えづらい問題の修正

Version 1.1.3p1
- FIX : Unity2018.1 : 'Configure Avatar'のエラーを修正

Version 1.1.3
- ADD : MagicaVoxel 0.99.1 World Editor(マルチオブジェクト)対応
- ADD : VoxelChunksObject : 完全に更新する"Refresh all chunks"ボタンを追加
- FIX : ColladaExporter : エラー処理修正
- FIX : VoxelChunksObject : Prefab選択でのRefresh操作を無効化
- FIX : 小文字ではない拡張子の場合のインポートエラー

Version 1.1.2p1
- FIX : Explosion : 65000以上の頂点数になる場合にMeshを分割しないよう対応 (Unity 2017.3以降のみ)
- FIX : DaeExporter : '_Color'プロパティがないマテリアルでのエラー修正

Version 1.1.2
- ADD : ScriptedImporter対応
- ADD : BonePosition : Scaling All
- ADD : BonePosition : 回転の設定を可能に変更
- ADD : VoxelStructure
- ADD : Optimize : Combine faces
- ADD : Examples : EditMesh, CreateCubes
- FIX : BonePosition : Humanoid設定後の位置変更が戻る不具合

Version 1.1.1p3
- FIX : .Net 4.6使用時にインポートできないことがある問題の修正

Version 1.1.1p2
- FIX : Explosion : Settingsの内容が実行時に反映されていなかった問題の修正

Version 1.1.1p1
- FIX : MagicaVoxel 0.99 alphaで保存されたvox読み出しでパレットとマテリアルが正しく読めない不具合修正 (単体オブジェクトデータのみ対応、複数オブジェクトデータの読み出し未対応)

Version 1.1.1
- ADD : 65000以上の頂点数になる場合に正常に表示されるよう対応 (Unity 2017.3以降のみ)
- FIX : Simple mode

Version 1.1.0
- ADD : Simple mode
- ADD : Voxel Skinned Animation Object : Animation Typeの変更でAnimatorやAnimationコンポーネントを自動的に追加するように対応
- FIX : HiDPI / Macbook Retina screen resolution support - ブラシ位置ずれ問題

Version 1.0.9p6
- FIX : ColladaExporter : Animation and Specular color
- FIX : Edit Bone Animation : Mirror側がアニメーションに記録されない不具合

Version 1.0.9p5
- FIX : ColladaExporter : Animation and Material Color
- FIX : Gizmos : Selection Wireが表示されなくなる問題の修正

Version 1.0.9p4
- FIX : ColladaExporter

Version 1.0.9p3
- FIX : ColladaExporter
- FIX : Skeleton : 表示を修正

Version 1.0.9p2
- FIX : WeightPaint : Undo修正
- FIX : ColladaExporter : Animation Export

Version 1.0.9p1
- FIX : ColladaExporter : FootIK有効時の警告対応、FootIK有効をデフォルトに変更
- FIX : Skeleton : 表示を変更

Version 1.0.9
- ADD : ColladaExporter : Animation出力対応
- ADD : Unity5.6 or later : Mecanim Humanoid Upper Chest対応
- FIX : ColladaExporter : VoxelSkinnedObjectから出力したファイルのHierarchyが出力前と変わる問題の修正
- FIX : 動作環境をUnity5.4.0以上に変更

Version 1.0.8p3
- FIX : FrameAnimation : プロジェクト外のファイルの追加読み出し不具合

Version 1.0.8p2
- ADD : MagicaVoxelのマテリアル設定の一部を反映

Version 1.0.8p1
- FIX : 低解像度でポリゴンの隙間が見える問題の対策追加

Version 1.0.8
- ADD : Collada(dae)ファイル出力
- FIX : VoxelDataロードタイミングを変更
- FIX : ImportOffsetのScale反映バグ

Version 1.0.7p1
- FIX : Generate Low-Poly Texture
- FIX : Edit Bone Animation - Move
- FIX : Unity 5.6.0 support
- FIX : Explosion : Shader : UNITY_MATRIX_MVP -> UnityObjectToClipPos
 
Version 1.0.7
- ADD : EditorのWindow化
- ADD : Prefab作成簡易化
- FIX : Import Flag
- FIX : Explosion Material
- FIX : Unity5.5 support

Version 1.0.6.p3
- FIX : マニュアル

Version 1.0.6.p2
- ADD : Bone - "Remove This Bone"
- FIX : HiDPI / Macbook Retina screen resolution support - 位置がずれる問題

Version 1.0.6.p1
- ADD : ContextMenu - "Remove All Voxel Importer Compornent"
- FIX : Materials - "Load From Voxel File"
- FIX : Voxel Skinned Animation Object - Erase MeshFilter
- FIX : Voxel Skinned Animation Object のboneにVoxel Skinned Animation Objectがある場合の不具合
- FIX : Import Mode - Low Poly テクスチャ生成

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
- ADD : ルートボーンのウェイトプレビュー
- ADD : ウェイトプレビュー半透明表示
- ADD : Drag&Dropでのファイルオープン
- ADD : Component反映フラグ
- ADD : Contextメニュー "Save All Unsaved Assets" & "Reset All Assets"
- FIX : Undo

Version 1.0.4.p1
- ADD : ルートボーンの追加削除機能
- FIX : エラーチェック強化

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
- ADD : マルチマテリアル対応
- FIX : ミラーリング処理

Version 1.021
- ADD : Humanoid Avatarの設定画面
- ADD : メニュー折りたたみ関係

Version 1.02
- ADD : Mecanim Humanoid サポート
- ADD : Edit Weight Paint : Auto Normalize
- FIX : DisableAnimation & MirrorAnimationの高速化
- FIX : Bone Positionに意図しない変更が起きないよう対応

Version 1.011
- FIX : Weightのミラーリング処理
- FIX : Undo全般

Version 1.01
- ADD : "Voxel Chunks Object"
- FIX : Weightのミラーリング処理
- FIX : Undo全般
- FIX : BoneTemplate検索
- FIX : 競合するショートカットキーの削除

Version 1.00
- ファーストリリース
