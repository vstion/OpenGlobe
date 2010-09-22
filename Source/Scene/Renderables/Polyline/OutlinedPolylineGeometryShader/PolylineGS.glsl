﻿#version 330 
//
// (C) Copyright 2010 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//

layout(lines) in;
layout(triangle_strip, max_vertices = 8) out;

in vec4 gsColor[];
in vec4 gsOutlineColor[];

flat out vec4 fsColor;

uniform mat4 og_modelViewPerspectiveMatrix;
uniform mat4 og_viewportTransformationMatrix;
uniform mat4 og_viewportOrthographicMatrix;
uniform float og_perspectiveNearPlaneDistance;
uniform float u_fillDistance;
uniform float u_outlineDistance;

vec4 ClipToWindowCoordinates(vec4 v, mat4 viewportTransformationMatrix)
{
    v.xyz /= v.w;                                                  // normalized device coordinates
    v.xyz = (viewportTransformationMatrix * vec4(v.xyz, 1.0)).xyz; // window coordinates
    return v;
}

void ClipLineSegmentToNearPlane(
    float nearPlaneDistance, 
    mat4 modelViewPerspectiveMatrix,
    vec4 modelP0, 
    vec4 modelP1, 
    out vec4 clipP0, 
    out vec4 clipP1,
	out bool culledByNearPlane)
{
    clipP0 = modelViewPerspectiveMatrix * modelP0;
    clipP1 = modelViewPerspectiveMatrix * modelP1;
	culledByNearPlane = false;

    float distanceToP0 = clipP0.z + nearPlaneDistance;
    float distanceToP1 = clipP1.z + nearPlaneDistance;

    if ((distanceToP0 * distanceToP1) < 0.0)
    {
        float t = distanceToP0 / (distanceToP0 - distanceToP1);
        vec3 modelV = vec3(modelP0) + t * (vec3(modelP1) - vec3(modelP0));
        vec4 clipV = modelViewPerspectiveMatrix * vec4(modelV, 1);

        if (distanceToP0 < 0.0)
        {
            clipP0 = clipV;
        }
        else
        {
            clipP1 = clipV;
        }
    }
	else if (distanceToP0 < 0.0)
	{
	    culledByNearPlane = true;
	}
}

void main()
{
    vec4 clipP0;
    vec4 clipP1;
	bool culledByNearPlane;
    ClipLineSegmentToNearPlane(og_perspectiveNearPlaneDistance, 
		og_modelViewPerspectiveMatrix,
		gl_in[0].gl_Position, gl_in[1].gl_Position, clipP0, clipP1, culledByNearPlane);

	if (culledByNearPlane)
	{
		return;
	}

    vec4 windowP0 = ClipToWindowCoordinates(clipP0, og_viewportTransformationMatrix);
    vec4 windowP1 = ClipToWindowCoordinates(clipP1, og_viewportTransformationMatrix);

    vec2 direction = windowP1.xy - windowP0.xy;
    vec2 normal = normalize(vec2(direction.y, -direction.x));

    vec4 v0 = vec4(windowP0.xy - (normal * u_outlineDistance), -windowP0.z, 1.0);
    vec4 v1 = vec4(windowP1.xy - (normal * u_outlineDistance), -windowP1.z, 1.0);
    vec4 v2 = vec4(windowP0.xy - (normal * u_fillDistance), -windowP0.z, 1.0);
    vec4 v3 = vec4(windowP1.xy - (normal * u_fillDistance), -windowP1.z, 1.0);
    vec4 v4 = vec4(windowP0.xy + (normal * u_fillDistance), -windowP0.z, 1.0);
    vec4 v5 = vec4(windowP1.xy + (normal * u_fillDistance), -windowP1.z, 1.0);
    vec4 v6 = vec4(windowP0.xy + (normal * u_outlineDistance), -windowP0.z, 1.0);
    vec4 v7 = vec4(windowP1.xy + (normal * u_outlineDistance), -windowP1.z, 1.0);

    gl_Position = og_viewportOrthographicMatrix * v0;
    fsColor = gsOutlineColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v1;
    fsColor = gsOutlineColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v2;
    fsColor = gsOutlineColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v3;
    fsColor = gsOutlineColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v4;
    fsColor = gsColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v5;
    fsColor = gsColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v6;
    fsColor = gsOutlineColor[0];
    EmitVertex();

    gl_Position = og_viewportOrthographicMatrix * v7;
    fsColor = gsOutlineColor[0];
    EmitVertex();
}
