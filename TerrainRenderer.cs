using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace Dungeon;

public class TerrainRenderer(Game game) : DrawableGameComponent(game)
{
	private int _mapWidth;
	private int _mapHeight;
	private float _maxHeight = 0;
	private float[,] _heightmapData;

	private VertexBuffer _vertexBuffer;
	private IndexBuffer _indexBuffer;

	private BasicEffect _basicEffect;
	private Matrix _world = Matrix.CreateTranslation(0, 0, 0);
	private Matrix _view = Matrix.CreateLookAt(new Vector3(0, 0, 3), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
	private Matrix _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), 800f / 480f, 0.01f, 100f);
	private float _angle = 0f;

	public override void Initialize()
	{
		LoadContent();
	}

	protected override void LoadContent()
	{
		_basicEffect = new BasicEffect(GraphicsDevice);

		LoadHeightmap("Content/height.txt");
		GenerateVertices();
		GenerateIndices();
	}

	public override void Update(GameTime gameTime)
	{
		_angle += 0.01f;
		_view = Matrix.CreateLookAt(
			new Vector3(10 * (float)Math.Sin(_angle), 7, 10 * (float)Math.Cos(_angle)),
			new Vector3(0, 0, 0),
			Vector3.UnitY);
	}

	public override void Draw(GameTime gameTime)
	{

		_basicEffect.World = _world;
		_basicEffect.View = _view;
		_basicEffect.Projection = _projection;
		_basicEffect.VertexColorEnabled = true;

		GraphicsDevice.SetVertexBuffer(_vertexBuffer);
		GraphicsDevice.Indices = _indexBuffer;

		RasterizerState rasterizerState = new()
		{
			CullMode = CullMode.None
		};

		GraphicsDevice.RasterizerState = rasterizerState;

		foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexBuffer.IndexCount / 3);
		}
	}

	private void LoadHeightmap(string fileName)
	{
		try
		{
			string[] lines = File.ReadAllLines(fileName);
			_mapHeight = lines.Length;

			string[] firstLineData = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
			_mapWidth = firstLineData.Length;

			_heightmapData = new float[_mapHeight, _mapWidth];

			for (int y = 0; y < _mapHeight; y++)
			{
				string[] lineData = lines[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (lineData.Length != _mapWidth)
				{
					throw new InvalidDataException($"Row {y} in heightmap {fileName} has an incorrect number of values.");
				}

				for (int x = 0; x < _mapWidth; x++)
				{
					if (float.TryParse(lineData[x], out float height))
					{
						_heightmapData[y, x] = height;
						if (height > _maxHeight)
						{
							_maxHeight = height;
						}
					}

					else
					{
						throw new InvalidDataException($"Invalid float value at position {x} on row {y} of heightmap {fileName}.");
					}
				}
			}
		}

		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading heightmap {fileName}: {ex.Message}");
		}
	}

	private void GenerateVertices()
	{
		VertexPositionColor[] vertices = new VertexPositionColor[_mapHeight * _mapWidth];

		// TODO: In the future, we want these to be fixed numbers
		float centerX = (_mapWidth - 1) / 2f;
		float centerY = (_mapHeight - 1) / 2f;

		for (int y = 0; y < _mapHeight; y++)
		{
			for (int x = 0; x < _mapWidth; x++)
			{
				float height = _heightmapData[y, x];
				vertices[y * _mapWidth + x] = new VertexPositionColor(new Vector3(x - centerX, height, y - centerY), new Color(0, 0.2f + (height / _maxHeight), 0));
			}
		}

		_vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
		_vertexBuffer.SetData(vertices);
	}

	private void GenerateIndices()
	{
		int numQuadsX = _mapWidth - 1;
		int numQuadsZ = _mapHeight - 1;
		short[] indices = new short[numQuadsX * numQuadsZ * 6];
		int index = 0;

		for (int z = 0; z < numQuadsZ; z++)
		{
			for (int x = 0; x < numQuadsX; x++)
			{
				// Indices for the four vertices of the current quad
				short topLeft = (short)(z * _mapWidth + x);
				short topRight = (short)(z * _mapWidth + x + 1);
				short bottomLeft = (short)((z + 1) * _mapWidth + x);
				short bottomRight = (short)((z + 1) * _mapWidth + x + 1);

				// First triangle
				indices[index++] = topLeft;
				indices[index++] = bottomLeft;
				indices[index++] = topRight;

				// Second triangle
				indices[index++] = topRight;
				indices[index++] = bottomLeft;
				indices[index++] = bottomRight;
			}
		}

		_indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
		_indexBuffer.SetData(indices);
	}
}
