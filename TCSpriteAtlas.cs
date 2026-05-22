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
using UnityEngine.U2D;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;
using System.Reflection;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TextCore
{
	public class TCSpriteAtlas : SpriteAsset
	{
		//--------------------------------------------------------------------------
		// Settings
		//--------------------------------------------------------------------------
		[SerializeField] public SpriteAtlas spriteAtlas;

#if UNITY_EDITOR
		//--------------------------------------------------------------------------
		// Utils
		//--------------------------------------------------------------------------
		internal static Texture emptyTexture = null;

		//--------------------------------------------------------------------------
		// CachedSpriteSettings
		//--------------------------------------------------------------------------
		private struct CachedSpriteSettings
		{
			public float glyphScale;
			public float bearingXDelta;
			public float bearingYDelta;
			public float advanceDelta;
			public float characterScale;
			public uint unicode;
			public bool hasCustomGlyphScale;
			public bool hasCustomBearingX;
			public bool hasCustomBearingY;
			public bool hasCustomAdvance;
			public bool hasCustomCharacterScale;
			public bool hasCustomUnicode;
		}

		//--------------------------------------------------------------------------
		// CacheCustomSettings
		//--------------------------------------------------------------------------
		private Dictionary<string, CachedSpriteSettings> CacheCustomSettings()
		{
			if(emptyTexture == null)
				emptyTexture = new Texture2D(0, 0);
			var texture = typeof(SpriteAsset).GetField("m_SpriteAtlasTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.GetValue(this) as Texture;
			if(texture == null)
				SetField(this, "m_SpriteAtlasTexture", emptyTexture);

			var cache = new Dictionary<string, CachedSpriteSettings>();
			if(spriteGlyphTable == null || spriteCharacterTable == null)
				return cache;

			var glyphLookup = new Dictionary<uint, SpriteGlyph>();
			foreach(var glyph in spriteGlyphTable)
				glyphLookup[glyph.index] = glyph;

			foreach(var character in spriteCharacterTable) {
				if(string.IsNullOrEmpty(character.name))
					continue;
				if(!glyphLookup.TryGetValue(character.glyphIndex, out var glyph))
					continue;

				var settings = new CachedSpriteSettings();

				settings.glyphScale = glyph.scale;
				settings.hasCustomGlyphScale = !Mathf.Approximately(glyph.scale, 1.0f);

				settings.bearingXDelta = glyph.metrics.horizontalBearingX - 0.0f;
				settings.hasCustomBearingX = !Mathf.Approximately(settings.bearingXDelta, 0.0f);

				settings.bearingYDelta = glyph.metrics.horizontalBearingY - glyph.metrics.height;
				settings.hasCustomBearingY = !Mathf.Approximately(settings.bearingYDelta, 0.0f);

				settings.advanceDelta = glyph.metrics.horizontalAdvance - glyph.metrics.width;
				settings.hasCustomAdvance = !Mathf.Approximately(settings.advanceDelta, 0.0f);

				settings.characterScale = character.scale;
				settings.hasCustomCharacterScale = !Mathf.Approximately(character.scale, 1.0f);

				settings.unicode = character.unicode;
				settings.hasCustomUnicode = character.unicode != 0xFFFE;

				if(settings.hasCustomGlyphScale || settings.hasCustomBearingX ||
				   settings.hasCustomBearingY || settings.hasCustomAdvance ||
				   settings.hasCustomCharacterScale || settings.hasCustomUnicode) {
					cache[character.name] = settings;
				}
			}

			return cache;
		}

		//--------------------------------------------------------------------------
		// RestoreCustomSettings
		//--------------------------------------------------------------------------
		private void RestoreCustomSettings(SpriteAsset atlas, Dictionary<string, CachedSpriteSettings> cache)
		{
			if(cache == null || cache.Count == 0)
				return;
			if(atlas.spriteGlyphTable == null || atlas.spriteCharacterTable == null)
				return;

			var glyphLookup = new Dictionary<uint, SpriteGlyph>();
			foreach(var glyph in atlas.spriteGlyphTable)
				glyphLookup[glyph.index] = glyph;

			foreach(var character in atlas.spriteCharacterTable) {
				if(string.IsNullOrEmpty(character.name))
					continue;
				if(!cache.TryGetValue(character.name, out var settings))
					continue;
				if(!glyphLookup.TryGetValue(character.glyphIndex, out var glyph))
					continue;

				if(settings.hasCustomGlyphScale)
					glyph.scale = settings.glyphScale;

				if(settings.hasCustomBearingX || settings.hasCustomBearingY || settings.hasCustomAdvance) {
					var m = glyph.metrics;
					float bearingX = settings.hasCustomBearingX ? settings.bearingXDelta : m.horizontalBearingX;
					float bearingY = settings.hasCustomBearingY ? (m.height + settings.bearingYDelta) : m.horizontalBearingY;
					float advance = settings.hasCustomAdvance ? (m.width + settings.advanceDelta) : m.horizontalAdvance;
					glyph.metrics = new GlyphMetrics(m.width, m.height, bearingX, bearingY, advance);
				}

				if(settings.hasCustomCharacterScale)
					character.scale = settings.characterScale;

				if(settings.hasCustomUnicode)
					character.unicode = settings.unicode;
			}
		}

		//--------------------------------------------------------------------------
		// Clear
		//--------------------------------------------------------------------------
		public void Clear()
		{
			SetField(this, "m_Version", "");

			if(emptyTexture == null)
				emptyTexture = new Texture2D(0, 0);
			SetSpriteSheet(this, emptyTexture);
			SetField(this, "m_SpriteCharacterTable", new List<SpriteCharacter>());
			SetField(this, "m_SpriteGlyphTable", new List<SpriteGlyph>());
			SetField(this, "m_SpriteCharacterLookup", new Dictionary<uint, SpriteCharacter>());
			fallbackSpriteAssets = new();

			// Clear assets
			foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)))
				if(asset != this)
					AssetDatabase.RemoveObjectFromAsset(asset);
			material = null;
			SetSpriteSheet(this, emptyTexture);
			AssetDatabase.SaveAssetIfDirty(this);

			UpdateLookupTables();
		}

		//--------------------------------------------------------------------------
		// UpdateSpriteData
		//--------------------------------------------------------------------------
		public void UpdateSpriteData()
		{
			// Cache customized settings BEFORE clearing
			var cachedSettings = CacheCustomSettings();
			var fallbackCaches = new Dictionary<string, Dictionary<string, CachedSpriteSettings>>();
			if(fallbackSpriteAssets != null) {
				foreach(var fallback in fallbackSpriteAssets) {
					if(fallback is TCSpriteAtlas fallbackAtlas)
						fallbackCaches[fallback.name] = fallbackAtlas.CacheCustomSettings();
				}
			}

			// Cache old assets
			Dictionary<string, object> oldLookup = new();
			foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)))
				oldLookup[asset.name] = asset;

			// Clear existing data
			Clear();

			// Add all atlas textures
			int atlasIndex = -1;
			var spriteAtlasPath = AssetDatabase.GetAssetPath(spriteAtlas);
			foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(spriteAtlasPath)) {
				if(asset is Texture2D atlasTexture) {
					// Create a new SpriteAsset if needed
					atlasIndex++;
					TCSpriteAtlas atlasAsset = atlasIndex == 0 ? this : null;
					if(atlasAsset == null) {
						string atlasName = spriteAtlas.name + "_" + atlasIndex;
						atlasAsset = oldLookup.ContainsKey(atlasName) ? oldLookup[atlasName] as TCSpriteAtlas : null;
						if(atlasAsset == null)
							atlasAsset = ScriptableObject.CreateInstance<TCSpriteAtlas>();
						atlasAsset.Clear();


						if(fallbackSpriteAssets == null)
							fallbackSpriteAssets = new();
						fallbackSpriteAssets.Add(atlasAsset);
						atlasAsset.name = atlasName;
						AssetDatabase.AddObjectToAsset(atlasAsset, this);
					}

					// Add texture and update material
					SetSpriteSheet(atlasAsset, atlasTexture);
					TMPro.ShaderUtilities.GetShaderPropertyIDs();
					Shader shader = Shader.Find("TextMeshPro/Sprite");

					string materialName = ((atlasIndex == 0) ? this.name : spriteAtlas.name + "_" + atlasIndex) + "_material";
					atlasAsset.material = oldLookup.ContainsKey(materialName) ? oldLookup[materialName] as Material : null;
					if(atlasAsset.material == null)
						atlasAsset.material = new Material(shader);
					atlasAsset.material.SetTexture(TMPro.ShaderUtilities.ID_MainTex, atlasAsset.spriteSheet);
					atlasAsset.material.name = materialName;
					AssetDatabase.AddObjectToAsset(atlasAsset.material, this);

					// Setup atlas
					SetField(atlasAsset, "m_Version", "1.1.0");
					atlasAsset.hashCode = TMPro.TMP_TextUtilities.GetSimpleHashCode(atlasAsset.name);

					// Add sprites
					AddSprites(atlasAsset);

					// Restore custom settings
					if(atlasIndex == 0) {
						RestoreCustomSettings(atlasAsset, cachedSettings);
					}
					else {
						string atlasName = spriteAtlas.name + "_" + atlasIndex;
						if(fallbackCaches.TryGetValue(atlasName, out var fallbackCache))
							RestoreCustomSettings(atlasAsset, fallbackCache);
					}
				}
			}

			// Save
			AssetDatabase.SetMainObject(this, AssetDatabase.GetAssetPath(this));
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssetIfDirty(this);

			// Notify
			if(fallbackSpriteAssets != null)
				foreach(var subAsset in fallbackSpriteAssets)
					TextEventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, subAsset);
			TextEventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, this);
		}

		//--------------------------------------------------------------------------
		// AddSprites
		//--------------------------------------------------------------------------
		private void AddSprites(SpriteAsset atlas)
		{
			// Get sprite sheet
			Texture2D spriteSheet = atlas.spriteSheet as Texture2D;
			if(spriteSheet == null)
				return;

			// Get all sprites
			var sprites = new Sprite[spriteAtlas.spriteCount];
			spriteAtlas.GetSprites(sprites);

			// Add sprites to sprite atlas
			uint spriteIndex = 0xFFFFFFFF;
			foreach(var sprite in sprites) {
				// Grab Sprite information
				Texture2D spriteTexture;
				Vector2[] spriteUV;
				try {
					spriteTexture = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, true);
					spriteUV = UnityEditor.Sprites.SpriteUtility.GetSpriteUVs(sprite, true);
				}
				catch {
					Debug.LogError("Failed to get sprite texture or UVs for sprite " + sprite.name + " usually happens when the atlas is not baked yet, bake it and try again.");
					continue;
				}
				// Make sure we got valid information and that the sprite is for the current atlas
				if(spriteTexture == null || spriteUV == null || spriteUV.Length < 2 || spriteTexture != spriteSheet)
					continue;
				spriteIndex++;

				// Find texture coordinates
				Vector2 min = spriteUV[0];
				Vector2 max = spriteUV[0];
				foreach(var uv in spriteUV) {
					min.x = Mathf.Min(min.x, uv.x);
					min.y = Mathf.Min(min.y, uv.y);
					max.x = Mathf.Max(max.x, uv.x);
					max.y = Mathf.Max(max.y, uv.y);
				}
				var UVRect = new Rect(min.x * spriteSheet.width, min.y * spriteSheet.height, (max.x - min.x) * spriteSheet.width, (max.y - min.y) * spriteSheet.height);
				var scale = new Vector2(spriteSheet.width / spriteSheet.width, spriteSheet.height / spriteSheet.height);

				// Add glyph
				var glyph = new SpriteGlyph();
				glyph.index = spriteIndex;
				glyph.metrics = new GlyphMetrics(UVRect.width, UVRect.height, 0.0f, UVRect.height * scale.y, UVRect.width * scale.x);
				glyph.glyphRect = new GlyphRect(UVRect);
				glyph.scale = 1.0f;
				glyph.sprite = sprite;
				atlas.spriteGlyphTable.Add(glyph);

				// Add character
				string characterName = sprite.name;
				if(characterName.EndsWith("(Clone)"))
					characterName = characterName.Substring(0, characterName.Length - "(Clone)".Length);
				var character = new SpriteCharacter(0xFFFE, glyph);
				character.scale = 1.0f;
				character.name = characterName;
				atlas.spriteCharacterTable.Add(character);
			}

			atlas.SortGlyphTable();
			atlas.UpdateLookupTables();
		}

		//--------------------------------------------------------------------------
		// Reflection
		//--------------------------------------------------------------------------
		static private void SetSpriteSheet(SpriteAsset atlas, Texture texture)
		{
			typeof(SpriteAsset).GetProperty(nameof(SpriteAsset.spriteSheet), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.SetValue(atlas, texture);
		}
		static internal void SetField(SpriteAsset atlas, string name, object value)
		{
			typeof(SpriteAsset).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.SetValue(atlas, value);
		}
#endif // UNITY_EDITOR
	}
}