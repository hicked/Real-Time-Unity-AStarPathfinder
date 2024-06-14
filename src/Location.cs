using UnityEngine;
using System;

public class Location : IEquatable<Location> { // Extends IEquatable to be able to perform conditional statement easier
    // See README.md for description of all Location Fields (x, y, z, g, h, f)
    public float x, y, z, g, h, f;
    
    // Vector3 representation of the Location Object
    public Vector3 vector;

    // Constructor, how to initialize a new Location object
    public Location(Vector3 position, float g, float h) {
        this.x = position.x;
        this.y = position.y;
        this.z = position.z;
        this.g = g;
        this.h = h;
        this.f = g + h;
        this.vector = new Vector3(x,y,z);
    }

    public bool Equals(Location other) {
        return this.x == other.x && this.y == other.y && this.z == other.z;
    }
}
