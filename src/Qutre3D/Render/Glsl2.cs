namespace Qutre3D.Render;

public static partial class Glsl
{
    public const string PostFrag = """
        #version 330 core
        in vec2 vUv;
        out vec4 FragColor;

        uniform sampler2D uScene;
        uniform sampler2D uBloom;

        uniform vec2  uResolution;
        uniform vec2  uSunScreen;
        uniform vec3  uShaftColor;
        uniform vec3  uTint;

        uniform float uExposure;
        uniform float uBloomStrength;
        uniform float uGodRays;
        uniform float uGrain;
        uniform float uVignette;
        uniform float uSaturation;
        uniform float uContrast;
        uniform float uChromatic;
        uniform float uTime;

        float ph(vec2 p) { return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453); }

        vec3 tonemap(vec3 x)
        {
            return clamp((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0.0, 1.0);
        }

        void main()
        {
            vec2 uv = vUv;
            vec2 d = uv - 0.5;

            float ca = uChromatic * 0.0035 * dot(d, d);
            vec3 c;
            c.r = texture(uScene, uv + d * ca).r;
            c.g = texture(uScene, uv).g;
            c.b = texture(uScene, uv - d * ca).b;

            if (uGodRays > 0.001)
            {
                vec2 delta = (uSunScreen - uv) * 0.28;
                vec3 shaft = vec3(0.0);
                float wsum = 0.0;
                for (int i = 0; i < 10; i++)
                {
                    float t = float(i) / 9.0;
                    float w = 1.0 - t;
                    shaft += texture(uBloom, clamp(uv + delta * t, 0.0, 1.0)).rgb * w;
                    wsum += w;
                }
                shaft /= max(wsum, 0.0001);
                c += shaft * uGodRays * uShaftColor;
            }

            c += texture(uBloom, uv).rgb * uBloomStrength;
            c *= uExposure;
            c = tonemap(c);

            float lum = dot(c, vec3(0.2126, 0.7152, 0.0722));
            c = mix(vec3(lum), c, uSaturation);
            c = (c - 0.5) * uContrast + 0.5;
            c *= uTint;

            float vig = smoothstep(1.30, 0.30, length(d) * 1.55);
            c *= mix(1.0, vig, uVignette);

            float g = ph(uv * uResolution + fract(uTime) * 137.0) - 0.5;
            c += g * uGrain;
            c += (ph(uv * uResolution * 1.71) - 0.5) * 0.005;

            FragColor = vec4(clamp(c, 0.0, 1.0), 1.0);
        }
        """;

    public const string BrightFrag = """
        #version 330 core
        in vec2 vUv;
        out vec4 FragColor;
        uniform sampler2D uSource;
        uniform float uThreshold;

        void main()
        {
            vec3 c = texture(uSource, vUv).rgb;
            float l = max(c.r, max(c.g, c.b));
            float k = smoothstep(uThreshold, uThreshold + 0.7, l);
            FragColor = vec4(c * k, 1.0);
        }
        """;

    public const string BlurFrag = """
        #version 330 core
        in vec2 vUv;
        out vec4 FragColor;
        uniform sampler2D uSource;
        uniform vec2 uDirection;

        void main()
        {
            vec3 c = texture(uSource, vUv).rgb * 0.227027;
            c += (texture(uSource, vUv + uDirection * 1.3846).rgb +
                  texture(uSource, vUv - uDirection * 1.3846).rgb) * 0.3162162;
            c += (texture(uSource, vUv + uDirection * 3.2308).rgb +
                  texture(uSource, vUv - uDirection * 3.2308).rgb) * 0.0702702;
            FragColor = vec4(c, 1.0);
        }
        """;

    public const string DustVert = """
        #version 330 core
        layout(location = 0) in vec3  aPos;
        layout(location = 1) in float aSeed;

        uniform mat4  uViewProj;
        uniform vec3  uCameraPos;
        uniform float uTime;
        uniform float uDensity;

        out float vAlpha;
        out float vSeed;

        void main()
        {
            vec3 p = aPos;
            float t = uTime * 0.25 + aSeed * 40.0;
            p.x += sin(t * 0.9 + aSeed * 6.28) * 2.4;
            p.z += cos(t * 0.7 + aSeed * 9.42) * 2.4;
            p.y += sin(t * 0.4 + aSeed * 3.14) * 1.1;

            gl_Position = uViewProj * vec4(p, 1.0);

            float d = length(p - uCameraPos);
            gl_PointSize = clamp(150.0 / max(d, 0.001), 1.0, 20.0);
            vAlpha = smoothstep(190.0, 10.0, d) * (0.22 + 0.78 * fract(aSeed * 7.13)) * uDensity;
            vSeed = aSeed;
        }
        """;

    public const string DustFrag = """
        #version 330 core
        in float vAlpha;
        in float vSeed;
        out vec4 FragColor;
        uniform vec3 uDustColor;

        void main()
        {
            vec2 d = gl_PointCoord - 0.5;
            float a = smoothstep(0.5, 0.0, length(d));
            a *= a;
            FragColor = vec4(uDustColor * (0.55 + 0.45 * vSeed), a * vAlpha);
        }
        """;

    public const string LineVert = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        uniform mat4 uModel;
        uniform mat4 uViewProj;
        out vec3 vWorld;

        void main()
        {
            vec4 wp = uModel * vec4(aPos, 1.0);
            vWorld = wp.xyz;
            gl_Position = uViewProj * wp;
        }
        """;

    public const string LineFrag = """
        #version 330 core
        in vec3 vWorld;
        out vec4 FragColor;

        uniform vec3  uColor;
        uniform float uAlpha;
        uniform vec3  uCameraPos;
        uniform float uFadeStart;
        uniform float uFadeEnd;

        void main()
        {
            float d = length(vWorld - uCameraPos);
            float fade = 1.0 - smoothstep(uFadeStart, uFadeEnd, d);
            FragColor = vec4(uColor, uAlpha * fade);
        }
        """;
}
