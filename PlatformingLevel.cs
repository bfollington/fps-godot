using Godot;
using System;

public partial class PlatformingLevel : Node3D
{
	private const float GridSize = 1.0f;
	
	public override void _Ready()
	{
		// Base floor with collision
		AddFloor();
		
		// Add platforms
		AddPlatform(new Vector3(5, 2, 0), new Vector3(3, 0.5f, 3));
		AddPlatform(new Vector3(8, 4, 3), new Vector3(3, 0.5f, 3));
		AddPlatform(new Vector3(12, 6, 0), new Vector3(3, 0.5f, 3));
		
		// Add ramps
		AddRamp(new Vector3(-5, 0, 0), new Vector3(6, 2, 3), 20); // Gentle slope
		AddRamp(new Vector3(0, 0, -8), new Vector3(3, 4, 3), 35); // Steeper ramp
		AddSpiralRamp(new Vector3(-10, 0, -10), 8, 6, 360); // Spiral ramp
		
		// Light
		var light = new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-45, -45, 0),
			LightEnergy = 2.0f,
			ShadowEnabled = true
		};
		AddChild(light);
	}
	
	private void AddFloor()
	{
		var floorNode = new StaticBody3D();
		
		var floorCSG = new CsgBox3D
		{
			Size = new Vector3(50, 1, 50),
			Position = new Vector3(0, -0.5f, 0)
		};
		
		var floorCollision = new CollisionShape3D
		{
			Shape = new BoxShape3D
			{
				Size = new Vector3(50, 1, 50)
			},
			Position = new Vector3(0, -0.5f, 0)
		};
		
		floorNode.AddChild(floorCSG);
		floorNode.AddChild(floorCollision);
		AddChild(floorNode);
	}
	
	private void AddPlatform(Vector3 position, Vector3 size)
	{
		var platformNode = new StaticBody3D
		{
			Position = position
		};
		
		var platformCSG = new CsgBox3D
		{
			Size = size,
			Material = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.8f, 0.8f, 0.8f)
			}
		};
		
		var platformCollision = new CollisionShape3D
		{
			Shape = new BoxShape3D
			{
				Size = size
			}
		};
		
		platformNode.AddChild(platformCSG);
		platformNode.AddChild(platformCollision);
		AddChild(platformNode);
	}
	
	private void AddRamp(Vector3 position, Vector3 size, float angleDegrees)
	{
		var rampNode = new StaticBody3D
		{
			Position = position
		};
		
		// Create the ramp visual
		var rampCSG = new CsgBox3D
		{
			Size = size,
			Material = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.7f, 0.7f, 0.8f)
			},
			// Rotate around Z axis for a ramp
			RotationDegrees = new Vector3(0, 0, -angleDegrees)
		};
		
		// Create matching collision
		var rampCollision = new CollisionShape3D
		{
			Shape = new BoxShape3D
			{
				Size = size
			},
			RotationDegrees = new Vector3(0, 0, -angleDegrees)
		};
		
		rampNode.AddChild(rampCSG);
		rampNode.AddChild(rampCollision);
		AddChild(rampNode);
	}
	
	private void AddSpiralRamp(Vector3 position, float radius, float height, float totalDegrees)
	{
		var spiralNode = new Node3D
		{
			Position = position
		};
		
		int segments = (int)(totalDegrees / 45); // A segment every 45 degrees
		float heightPerSegment = height / segments;
		float degreesPerSegment = totalDegrees / segments;
		
		for (int i = 0; i < segments; i++)
		{
			float angle = Mathf.DegToRad(i * degreesPerSegment);
			float nextAngle = Mathf.DegToRad((i + 1) * degreesPerSegment);
			
			// Calculate positions
			float x = Mathf.Cos(angle) * radius;
			float z = Mathf.Sin(angle) * radius;
			float nextX = Mathf.Cos(nextAngle) * radius;
			float nextZ = Mathf.Sin(nextAngle) * radius;
			
			// Create ramp segment
			var segmentNode = new StaticBody3D
			{
				Position = new Vector3(
					(x + nextX) / 2,
					i * heightPerSegment,
					(z + nextZ) / 2
				)
			};
			
			// Calculate segment size and rotation
			float segmentLength = Mathf.Sqrt(
				Mathf.Pow(nextX - x, 2) + 
				Mathf.Pow(nextZ - z, 2)
			);
			
			var segmentCSG = new CsgBox3D
			{
				Size = new Vector3(segmentLength, 0.5f, 2f),
				Material = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.7f, 0.8f, 0.7f)
				},
				RotationDegrees = new Vector3(
					-Mathf.RadToDeg(Mathf.Atan2(heightPerSegment, segmentLength)),
					Mathf.RadToDeg(angle) + 90,
					0
				)
			};
			
			var segmentCollision = new CollisionShape3D
			{
				Shape = new BoxShape3D
				{
					Size = new Vector3(segmentLength, 0.5f, 2f)
				}
			};
			segmentCollision.RotationDegrees = segmentCSG.RotationDegrees;
			
			segmentNode.AddChild(segmentCSG);
			segmentNode.AddChild(segmentCollision);
			spiralNode.AddChild(segmentNode);
		}
		
		AddChild(spiralNode);
	}
}
