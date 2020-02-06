﻿using Unity.Entities;
using Unity.Mathematics;

namespace Steering
{
	[GenerateAuthoringComponent]
	public struct MovingData : IComponentData
	{
		/// <summary>
		/// 速度
		/// </summary>
		public float2 velocity;
		/// <summary>
		/// 前向量
		/// </summary>
		public float2 forward;
		/// <summary>
		/// 右向量
		/// </summary>
		public float2 right;
		/// <summary>
		/// 速率
		/// </summary>
		public float speed;

		/// <summary>
		/// 最大速率
		/// </summary>
		public float maxSpeed;
		/// <summary>
		/// 最大受力
		/// </summary>
		public float maxForce;
		/// <summary>
		/// 最大角动量
		/// </summary>
		public float maxTurnRate;
		/// <summary>
		/// 重量
		/// </summary>
		public float mass;
		/// <summary>
		/// 视野距离
		/// </summary>
		public float viewDistance;
	}
}
