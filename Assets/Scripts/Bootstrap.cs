﻿using Steering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Environment = Steering.Environment;

public class Bootstrap : MonoBehaviour
{
	//vehicles
	public GameObject vehiclePrefab;
	[HideInInspector]
	public Mesh mesh;
	public Material material;
	public int amount;

	//walls
	public GameObject wallPrefab;
	public float2 minXY;
	public float2 maxXY;
	public float wallBorderSize;

	//quadrant
	public int numCellX;
	public int numCellZ;

	private void Start()
	{
		Environment.minXY = this.minXY;
		Environment.maxXY = this.maxXY;
		Environment.numCell = new int2( this.numCellX, this.numCellZ );
		var worldSize = Environment.maxXY - Environment.minXY;
		Environment.cellSize = new float2( worldSize.x / Environment.numCell.x, worldSize.y / Environment.numCell.y );

		this.mesh = CreateMesh();
		this.CreateCells();
		this.CreateWalls();
	}

	private void OnDestroy()
	{
		Destroy( this.mesh );
	}

	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.X ) )
		{
			this.SpawnPrefab();
		}
		this.DebugDrawCells();
	}

	private void CreateCells()
	{
		var manager = Environment.world.EntityManager;

		var archeType = manager.CreateArchetype(
			typeof( CellData )
		);

		int k = 0;
		for ( int i = 0; i < Environment.numCell.y; i++ )
		{
			for ( int j = 0; j < Environment.numCell.x; j++ )
			{
				var entity = manager.CreateEntity( archeType );
				manager.SetComponentData( entity, new CellData
				{
					index = k++,
					center = new float2( Environment.minXY.x + Environment.cellSize.x * 0.5f + Environment.cellSize.x * j,
					Environment.minXY.y + Environment.cellSize.y * 0.5f + Environment.cellSize.y * i ),
					extends = Environment.cellSize * 0.5f
				} );
			}
		}
	}

	private void CreateWalls()
	{
		float2[] wallDatas = new[]
		{
			//right
			new float2(this.maxXY.x+this.wallBorderSize, this.minXY.y),
			new float2(this.minXY.x-this.wallBorderSize, this.minXY.y-this.wallBorderSize),
			//bottom
			new float2(this.maxXY.x+this.wallBorderSize, this.maxXY.y),
			new float2(this.maxXY.x, this.minXY.y),
			//left
			new float2(this.minXY.x-this.wallBorderSize, this.maxXY.y),
			new float2(this.maxXY.x+this.wallBorderSize, this.maxXY.y+this.wallBorderSize),
			//top
			new float2(this.minXY.x-this.wallBorderSize, this.minXY.y),
			new float2(this.minXY.x, this.maxXY.y),
		};

		var manager = Environment.world.EntityManager;

		var setting = GameObjectConversionSettings.FromWorld( Environment.world, new BlobAssetStore() );
		var entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( this.wallPrefab, setting );

		var wallCount = wallDatas.Length / 2;
		var walls = new NativeArray<Entity>( wallCount, Allocator.Temp );
		manager.Instantiate( entityPrefab, walls );
		manager.DestroyEntity( entityPrefab );

		for ( int i = 0; i < wallCount; i++ )
		{
			var wall = walls[i];
			var from = wallDatas[i * 2 + 0];
			var to = wallDatas[i * 2 + 1];
			var size = from - to;
			var normal = math.normalize( size );
			normal = new float2( normal.y, -normal.x );
			var center = ( from + to ) / 2.0f;

			manager.SetComponentData( wall, new WallData
			{
				from = from,
				to = to,
				size = size,
				normal = normal,
				center = center
			} );
			manager.SetComponentData( wall, new Translation { Value = new float3( center.x, 0, center.y ) } );
			manager.SetComponentData( wall, new Rotation { Value = quaternion.identity } );
			manager.AddComponentData( wall, new NonUniformScale { Value = new float3( size.x, 1, size.y ) } );
		}

		walls.Dispose();
	}

	private void SpawnPrefab()
	{
		var manager = Environment.world.EntityManager;

		var setting = GameObjectConversionSettings.FromWorld( Environment.world, new BlobAssetStore() );
		var entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( this.vehiclePrefab, setting );

		var vehicles = new NativeArray<Entity>( this.amount, Allocator.Temp );
		manager.Instantiate( entityPrefab, vehicles );
		manager.DestroyEntity( entityPrefab );

		for ( int i = 0; i < this.amount; i++ )
		{
			var vehicle = vehicles[i];

			var radius = manager.GetComponentData<EntityData>( vehicle ).radius;
			var wallBorderSize = this.wallBorderSize * 0.5f;
			var position = new Vector3( UnityEngine.Random.Range( this.minXY.x + wallBorderSize + radius, this.maxXY.x - wallBorderSize - radius ), 0,
				UnityEngine.Random.Range( this.minXY.y + wallBorderSize + radius, this.maxXY.y - wallBorderSize - radius ) );
			var rotation = quaternion.AxisAngle( new float3( 0, 1, 0 ), math.radians( UnityEngine.Random.Range( 0, 360 ) ) );
			var forward = math.forward( rotation );

			manager.AddBuffer<NeighbourElement>( vehicle );
			manager.AddBuffer<ObstacleElement>( vehicle );
			manager.SetComponentData( vehicle, new Translation { Value = position } );
			manager.SetComponentData( vehicle, new Rotation { Value = rotation } );
			manager.AddSharedComponentData( vehicle, new RenderMesh { mesh = this.mesh, material = this.material, castShadows = UnityEngine.Rendering.ShadowCastingMode.Off } );
			manager.SetComponentData( vehicle, new EntityData { position = new float2( position.x, position.z ), radius = radius } );
			var movingData = manager.GetComponentData<MovingData>( vehicle );
			movingData.forward = new float2( forward.x, forward.z );
			movingData.right = new float2( forward.z, -forward.x );
			movingData.velocity = movingData.forward;
			manager.SetComponentData( vehicle, movingData );
		}

		vehicles.Dispose();
	}

	private static Mesh CreateMesh()
	{
		var vertices = new Vector3[3];
		vertices[0] = new Vector3( -0.25f, 0, -0.25f );
		vertices[1] = new Vector3( 0f, 0, 0.5f );
		vertices[2] = new Vector3( 0.25f, 0, -0.25f );
		var normals = new Vector3[3];
		normals[0] = new Vector3( 0, 1, 0 );
		normals[1] = new Vector3( 0, 1, 0 );
		normals[2] = new Vector3( 0, 1, 0 );
		var triangles = new int[3];
		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		var mesh = new Mesh { vertices = vertices, normals = normals, triangles = triangles };
		return mesh;
	}

	private void DebugDrawCells()
	{
		var manager = Environment.world.EntityManager;
		var cellQuery = manager.CreateEntityQuery( typeof( CellData ) );
		var cellDataArray = cellQuery.ToComponentDataArray<CellData>( Allocator.TempJob );
		var count = cellDataArray.Length;
		for ( int i = 0; i < count; i++ )
		{
			var cellData = cellDataArray[i];
			var min = cellData.center - cellData.extends;
			var max = cellData.center + cellData.extends;
			Debug.DrawLine( new Vector3( min.x, 0, min.y ), new Vector3( min.x, 0, max.y ) );
			Debug.DrawLine( new Vector3( min.x, 0, min.y ), new Vector3( max.x, 0, min.y ) );
			Debug.DrawLine( new Vector3( max.x, 0, max.y ), new Vector3( min.x, 0, max.y ) );
			Debug.DrawLine( new Vector3( max.x, 0, max.y ), new Vector3( max.x, 0, min.y ) );
		}
		cellDataArray.Dispose();
	}
}
