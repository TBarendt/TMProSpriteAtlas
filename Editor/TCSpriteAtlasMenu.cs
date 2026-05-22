//------------------------------------------------------------------------------
// MIT License
//
// Copyright (c) 2025 Tobias Barendt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//------------------------------------------------------------------------------
using System.IO;
using UnityEditor;
using UnityEngine.U2D;

namespace UnityEngine.TextCore
{
	public static class TCSpriteAssetMenu
	{
		//--------------------------------------------------------------------------
		// Create/Update Sprite Asset
		//--------------------------------------------------------------------------
		[MenuItem("Assets/Create/Text Core/SpriteAtlas Asset", true, 5000)]
		private static bool CreateAssetValidate() => Selection.activeObject is SpriteAtlas;
		[MenuItem("Assets/Create/Text Core/SpriteAtlas Asset", false, 5000)]
		private static void CreateAsset()
		{
			// Get the selected SpriteAtlas
			var spriteAtlas = (SpriteAtlas)Selection.activeObject;
			var spriteAtlasPath = AssetDatabase.GetAssetPath(spriteAtlas);

			// Create or load a asset
			var spriteAsset = AssetDatabase.LoadAssetAtPath<TCSpriteAtlas>(Path.GetDirectoryName(spriteAtlasPath) + "/" + spriteAtlas.name + "_TC.asset");
			if(spriteAsset == null) {
				spriteAsset = ScriptableObject.CreateInstance<TCSpriteAtlas>();
				AssetDatabase.CreateAsset(spriteAsset, Path.GetDirectoryName(spriteAtlasPath) + "/" + spriteAtlas.name + "_TC.asset");
			}
			if(TCSpriteAtlas.emptyTexture == null)
				TCSpriteAtlas.emptyTexture = new Texture2D(0, 0);
			TCSpriteAtlas.SetField(spriteAsset, "m_SpriteAtlasTexture", TCSpriteAtlas.emptyTexture);
			spriteAsset.spriteAtlas = spriteAtlas;
			spriteAsset.UpdateSpriteData();
		}
	}
}
