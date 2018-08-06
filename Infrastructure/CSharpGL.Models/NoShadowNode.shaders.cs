﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpGL
{
    public partial class NoShadowNode
    {
        private const string ambientVert = @"#version 150

in vec3 inPosition;

uniform mat4 mvpMat;

void main() {
    gl_Position = mvpMat * vec4(inPosition, 1.0);
}
";

        private const string ambientFrag = @"#version 150

uniform vec3 ambientColor;

out vec4 fragColor;

void main() {
	fragColor = vec4(ambientColor, 1.0);
}
";

        private const string blinnPhongVert = @"// Blinn-Phong-WorldSpace.vert
#version 150

in vec3 inPosition;
in vec3 inNormal;

// Declare an interface block.
out VS_OUT {
    vec3 position;
	vec3 normal;
} vs_out;

uniform mat4 mvpMat;
uniform mat4 modelMat;
uniform mat4 normalMat; // transpose(inverse(modelMat));

void main() {
    gl_Position = mvpMat * vec4(inPosition, 1.0);
    vec4 worldPos = modelMat * vec4(inPosition, 1.0);
	vs_out.position = worldPos.xyz;
	vs_out.normal = (normalMat * vec4(inNormal, 0)).xyz;
}
";
        private const string blinnPhongFrag = @"// Blinn-Phong-WorldSpace.frag
#version 150

struct Light {
    vec3 position;   // for directional light, meaningless.
    vec3 diffuse;
    vec3 specular;
    float constant;  // Attenuation.constant.
    float linear;    // Attenuation.linear.
    float quadratic; // Attenuation.quadratic.
	// direction from outer space to light source.
	vec3 direction;  // for point light, meaningless.
	// Note: We assume that spot light's angle ranges from 0 to 180 degrees.
    // cutOff = Cos(angle). angle ranges in [0, 90].
	float cutOff;    // for spot light, cutOff. for others, meaningless.
};

struct Material {
    vec3 diffuse;
    vec3 specular;
    float shiness;
};

uniform Light light;
uniform int lightUpRoutine; // 0: point light; 1: directional light; 2: spot light.

uniform Material material;

uniform vec3 eyePos;

uniform bool blinn = true;

in VS_OUT {
    vec3 position;
	vec3 normal;
} fs_in;

void LightUp(vec3 lightDir, vec3 normal, vec3 ePos, vec3 vPos, float shiness, out float diffuse, out float specular) {
    // Diffuse factor
    diffuse = max(dot(lightDir, normal), 0);

    // Specular factor
    vec3 eyeDir = normalize(ePos - vPos);
    specular = 0;
    if (blinn) {
        if (diffuse > 0) {
            vec3 halfwayDir = normalize(lightDir + eyeDir);
            specular = pow(max(dot(normal, halfwayDir), 0.0), shiness);
        }
    }
    else {
        if (diffuse > 0) {
            vec3 reflectDir = reflect(-lightDir, normal);
            specular = pow(max(dot(eyeDir, reflectDir), 0.0), shiness);
        }
    }
}

void PointLightUp(Light light, out float diffuse, out float specular) {
    vec3 Distance = light.position - fs_in.position;
    vec3 lightDir = normalize(Distance);
    vec3 normal = normalize(fs_in.normal); 
    float distance = length(Distance);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);
    
    LightUp(lightDir, normal, eyePos, fs_in.position, material.shiness, diffuse, specular);
    
    diffuse = diffuse * attenuation;
    specular = specular * attenuation;
}

void DirectionalLightUp(Light light, out float diffuse, out float specular) {
    vec3 lightDir = normalize(light.direction);
    vec3 normal = normalize(fs_in.normal); 
    LightUp(lightDir, normal, eyePos, fs_in.position, material.shiness, diffuse, specular);
}

// Note: We assume that spot light's angle ranges from 0 to 180 degrees.
void SpotLightUp(Light light, out float diffuse, out float specular) {
    vec3 Distance = light.position - fs_in.position;
    vec3 lightDir = normalize(Distance);
    vec3 centerDir = normalize(light.direction);
    float c = dot(lightDir, centerDir);// cut off at this point.
    if (c < light.cutOff) { // current point is outside of the cut off edge. 
        diffuse = 0; specular = 0;
    }
    else {
        vec3 normal = normalize(fs_in.normal); 
        float distance = length(Distance);
        float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);

        LightUp(lightDir, normal, eyePos, fs_in.position, material.shiness, diffuse, specular);
    
        diffuse = diffuse * attenuation;
        specular = specular * attenuation;
    }
}

out vec4 fragColor;

void main() {
    float diffuse = 0;
    float specular = 0;
	if (lightUpRoutine == 0) { PointLightUp(light, diffuse, specular); }
	else if (lightUpRoutine == 1) { DirectionalLightUp(light, diffuse, specular); }
	else if (lightUpRoutine == 2) { SpotLightUp(light, diffuse, specular); }
    else { diffuse = 0; specular = 0; }

	fragColor = vec4(diffuse * light.diffuse * material.diffuse + specular * light.specular * material.specular, 1.0);
}
";

    }
}