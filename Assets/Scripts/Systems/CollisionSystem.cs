﻿using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Steering
{
	[UpdateAfter( typeof( SteeringSystem ) )]
	public class CollisionSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			var dt = this.Time.DeltaTime;
			this.Entities.WithAll<VehicleData>().
			   ForEach( ( Entity vehicle, ref Unity.Transforms.Translation translation, ref EntityData entityData, in DynamicBuffer<NeighbourElement> neighbors ) =>
			   {
				   var awayForce = float2.zero;
				   var radius = entityData.radius;
				   var position = entityData.position;
				   var count = neighbors.Length;
				   for ( var i = 0; i < count; ++i )
				   {
					   var neighborData = neighbors[i].neighbourData;
					   if ( entityData.mass > neighborData.mass )
						   continue;
					   var radius2 = neighborData.radius;
					   var radiusBoth = radius + radius2;
					   var distanceSqrt = math.distancesq( position, neighborData.position );
					   if ( distanceSqrt < radiusBoth * radiusBoth )
					   {
						   var distance = math.sqrt( distanceSqrt );
						   var awayFactor = ( radiusBoth - distance ) / radius;
						   awayFactor *= awayFactor;
						   awayForce += ( position - neighborData.position ) * ( awayFactor / distance );
					   }
				   }
				   //  UnityEngine.Debug.DrawRay( new UnityEngine.Vector3( position.x, 0, position.y ),
				   //new UnityEngine.Vector3( awayForce.x, 0, awayForce.y ) );
				   entityData.position += awayForce * dt;
				   translation.Value = new float3( entityData.position.x, 0, entityData.position.y );
			   } ).Schedule();
		}
	}
}
