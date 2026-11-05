namespace Qutre3D.Render;

/// <summary>
/// Все шейдеры лежат прямо в коде — приложению не нужны внешние файлы.
/// Хочешь поиграть с картинкой? Правь строки здесь и пересобирай.
/// </summary>
public static partial class Glsl
{
    public const string FullscreenVert = """
        #version 330 core
        out vec2 vUv;
        void main()
        {
            vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
            vUv = p;
            gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    public const string Common = """
        #version 330 core

        // ---------- shared atmosphere ----------
        uniform vec3  uSunDir;
        uniform vec3  uSunColor;
        uniform float uSunIntensity;
        uniform vec3  uZenithColor;
        uniform vec3  uHorizonColor;
        uniform vec3  uGroundColor;
        uniform float uCloudCover;
        uniform float uStars;
        uniform float uTime;

        uniform vec3  uCameraPos;
        uniform vec3  uFogColor;
        uniform float uFogDensity;
        uniform float uFogHeightFalloff;

        // checker floor, used for reflections in chrome and glass
        uniform float uFloorReflect;
        uniform float uFloorY;
        uniform float uFloorTile;
        uniform vec3  uFloorDark;
        uniform vec3  uFloorLight;

        float qHash21(vec2 p)
        {
            vec3 p3 = fract(vec3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return fract((p3.x + p3.y) * p3.z);
        }

        float qNoise(vec2 p)
        {
            vec2 i = floor(p);
            vec2 f = fract(p);
            vec2 u = f * f * (3.0 - 2.0 * f);
            float a = qHash21(i);
            float b = qHash21(i + vec2(1.0, 0.0));
            float c = qHash21(i + vec2(0.0, 1.0));
            float d = qHash21(i + vec2(1.0, 1.0));
            return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
        }

        float qFbm(vec2 p, int octaves)
        {
            float v = 0.0;
            float amp = 0.5;
            float norm = 0.0;
            for (int i = 0; i < 7; i++)
            {
                if (i >= octaves) break;
                v += amp * qNoise(p);
                norm += amp;
                p = p * 2.03 + vec2(11.3, 7.7);
                amp *= 0.5;
            }
            return norm > 0.0 ? v / norm : 0.0;
        }

        vec3 qSky(vec3 dir, float sunDisc)
        {
            float y = dir.y;

            vec3 col = mix(uGroundColor, uHorizonColor, smoothstep(-0.18, 0.03, y));
            col = mix(col, uZenithColor, pow(clamp(y, 0.0, 1.0), 0.45));

            float mu = clamp(dot(dir, uSunDir), 0.0, 1.0);

            // sun halo (three scattering lobes)
            col += uSunColor * uSunIntensity *
                   (pow(mu, 5.0) * 0.16 + pow(mu, 42.0) * 0.38
                    + pow(mu, 600.0) * 0.85 * (0.35 + 0.65 * sunDisc));

            // horizon haze
            col += uHorizonColor * 0.16 * exp(-abs(y) * 14.0);

            // clouds: ray projected onto a plane
            if (uCloudCover > 0.001 && y > 0.0)
            {
                vec2 cuv = dir.xz / (y + 0.10);
                cuv = cuv * 0.42 + vec2(uTime * 0.0045, uTime * 0.0018);
                float n = qFbm(cuv * 1.2, 5);
                float cov = smoothstep(1.0 - uCloudCover, 1.0 - uCloudCover + 0.42, n);
                vec3 cloudCol = mix(uZenithColor * 0.60 + uHorizonColor * 0.35,
                                    uSunColor * 1.35, pow(mu, 2.5));
                cloudCol *= 0.75 + 0.5 * pow(max(dot(normalize(vec3(dir.x, 0.5, dir.z)), uSunDir), 0.0), 2.0);
                col = mix(col, cloudCol, cov * smoothstep(0.0, 0.05, y) * 0.92);
            }

            // stars
            if (uStars > 0.001 && y > 0.0)
            {
                vec3 sp = dir * 300.0;
                vec3 cell = floor(sp);
                float h = fract(sin(dot(cell, vec3(12.9898, 78.233, 37.719))) * 43758.5453);
                float star = smoothstep(0.9975, 1.0, h);
                float tw = 0.55 + 0.45 * sin(uTime * 1.3 + h * 91.0);
                col += vec3(0.85, 0.92, 1.0) * star * tw * uStars * smoothstep(0.0, 0.25, y);
            }

            // sun disc
            if (sunDisc > 0.0)
            {
                float disc = smoothstep(0.99955, 0.99985, mu);
                col += uSunColor * disc * 26.0 * uSunIntensity * sunDisc;
            }

            return max(col, vec3(0.0));
        }

        vec3 qFog(vec3 color, vec3 worldPos)
        {
            vec3 toP = worldPos - uCameraPos;
            float dist = length(toP);
            vec3 dir = toP / max(dist, 0.0001);

            float hAvg = 0.5 * (max(uCameraPos.y, 0.0) + max(worldPos.y, 0.0));
            float hFall = exp(-hAvg * uFogHeightFalloff);

            float amount = 1.0 - exp(-dist * uFogDensity * (0.30 + 0.70 * hFall));
            float scatter = pow(max(dot(dir, uSunDir), 0.0), 8.0);
            vec3 fogCol = uFogColor + uSunColor * uSunIntensity * scatter * 0.42;

            return mix(color, fogCol, clamp(amount, 0.0, 0.98));
        }

        // Sky reflection for metals and gloss: this is what makes chrome and
        // glass read as Bryce rather than as flat plastic.
        vec3 qReflectSky(vec3 n, vec3 v, float rough, vec3 worldPos, float metal)
        {
            vec3 r = reflect(-v, n);
            vec3 sharp;

            bool hitFloor = uFloorReflect > 0.5 && r.y < -0.015 && worldPos.y > uFloorY + 0.01 && metal > 0.35;
            if (hitFloor)
            {
                // mirror ray hits the checker floor: this is the signature
                // Bryce reflection on the lower half of a chrome ball
                float t = (uFloorY - worldPos.y) / r.y;
                vec2 hp = worldPos.xz + r.xz * t;
                vec2 tp = hp / max(uFloorTile, 0.1);

                // anti-aliased reflected checker: without this the mirror
                // image breaks into hard jagged staircases
                vec2 rw = fwidth(tp);
                vec2 rwc = clamp(rw, vec2(0.001), vec2(0.45));
                vec2 rf = fract(tp);
                float rsx = smoothstep(0.5 - rwc.x, 0.5 + rwc.x, rf.x);
                float rsy = smoothstep(0.5 - rwc.y, 0.5 + rwc.y, rf.y);
                float chk = abs(rsx - rsy);
                vec3 tile = mix(uFloorDark, uFloorLight, chk);

                // kill sparkle when the reflected checker gets denser than a pixel
                float rlod = smoothstep(0.55, 1.10, max(rw.x, rw.y));
                tile = mix(tile, mix(uFloorDark, uFloorLight, 0.5) * 0.85, rlod);

                float haze = 1.0 - exp(-t * 0.0035);
                sharp = mix(tile, uHorizonColor * 0.85, haze);
            }
            else
            {
                sharp = qSky(normalize(r), 0.0);

                // below the horizon a mirror should pick up the dark ground,
                // not a bright band of sky
                float below = smoothstep(-0.02, -0.30, r.y);
                sharp = mix(sharp, uGroundColor * 0.55, below);
            }

            vec3 dome = uGroundColor * 0.45 + uHorizonColor * 0.62 + uZenithColor * 0.62;
            float blur = clamp(rough * rough * 1.15, 0.0, 1.0);
            return mix(sharp, dome, blur);
        }

        vec3 qLight(vec3 albedo, vec3 n, vec3 v, float rough, float metal, vec3 worldPos)
        {
            float ndl = max(dot(n, uSunDir), 0.0);
            float ndv = clamp(dot(n, v), 0.0, 1.0);
            vec3 h = normalize(uSunDir + v);

            float specPower = mix(6.0, 1400.0, pow(1.0 - rough, 2.4));
            float spec = pow(max(dot(n, h), 0.0), specPower) * (0.05 + 0.95 * (1.0 - rough));

            vec3 skyAmb = mix(uGroundColor, uHorizonColor, clamp(n.y * 0.5 + 0.5, 0.0, 1.0)) * 0.78
                        + uZenithColor * 0.40;

            // diffuse dies on metals, leaving room for the reflection
            vec3 diffuse = albedo * (1.0 - metal * 0.88)
                         * (skyAmb * 1.15 + uSunColor * uSunIntensity * ndl * 1.25);

            // Fresnel: metals are almost mirror-like, dielectrics ~4-5 %
            float fres = pow(1.0 - ndv, 5.0);
            float f0 = mix(0.045, 0.94, metal);
            float F = f0 + (1.0 - f0) * fres;

            vec3 env = qReflectSky(n, v, rough * (1.0 - 0.55 * metal), worldPos, metal);
            vec3 tint = mix(vec3(1.0), albedo * 1.45 + 0.05, metal);

            vec3 col = diffuse + env * F * (0.30 + 1.25 * metal) * tint;
            col += albedo * metal * uSunColor * uSunIntensity * (0.05 + 0.08 * ndl);
            col += uSunColor * uSunIntensity * spec * (0.10 + 0.55 * (1.0 - rough)) * (0.45 + 0.85 * metal);
            col += pow(1.0 - ndv, 4.0) * uSunColor * 0.05 * uSunIntensity;

            return col;
        }
        """;

    public const string SkyFrag = """
        in vec2 vNdc;
        out vec4 FragColor;
        uniform mat4 uInvViewProj;

        void main()
        {
            vec4 p = uInvViewProj * vec4(vNdc, 1.0, 1.0);
            vec3 world = p.xyz / p.w;
            vec3 dir = normalize(world - uCameraPos);
            FragColor = vec4(qSky(dir, 1.0), 1.0);
        }
        """;

    public const string SkyVert = """
        #version 330 core
        out vec2 vNdc;
        void main()
        {
            vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
            vNdc = p * 2.0 - 1.0;
            gl_Position = vec4(vNdc, 1.0, 1.0);
        }
        """;

    public const string ObjectVert = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec2 aUv;
        layout(location = 3) in float aHeight;

        uniform mat4 uModel;
        uniform mat4 uViewProj;

        out vec3  vWorld;
        out vec3  vNormal;
        out vec2  vUv;
        out float vHeight;

        void main()
        {
            vec4 wp = uModel * vec4(aPos, 1.0);
            vWorld  = wp.xyz;
            vNormal = normalize(mat3(transpose(inverse(uModel))) * aNormal);
            vUv     = aUv;
            vHeight = aHeight;
            gl_Position = uViewProj * wp;
        }
        """;

    public const string ObjectFrag = """
        in vec3  vWorld;
        in vec3  vNormal;
        in vec2  vUv;
        in float vHeight;

        out vec4 FragColor;

        uniform vec3  uAlbedo;
        uniform vec3  uEmissive;
        uniform float uRoughness;
        uniform float uMetallic;
        uniform float uOpacity;

        void main()
        {
            vec3 n = normalize(vNormal);
            if (!gl_FrontFacing) n = -n;
            vec3 v = normalize(uCameraPos - vWorld);

            float ndv = clamp(dot(n, v), 0.0, 1.0);
            float fres = pow(1.0 - ndv, 3.0);

            vec3 col = qLight(uAlbedo, n, v, uRoughness, uMetallic, vWorld);
            col += uEmissive;

            float alpha = uOpacity;
            if (uOpacity < 0.999)
            {
                // glass: clear centre, dense glossy edges
                vec3 sheen = qReflectSky(n, v, uRoughness * 0.35, vWorld, uMetallic);
                col *= 0.34;
                col += sheen * (0.28 + 1.10 * fres);
                alpha = clamp(uOpacity * 0.62 + fres * 0.88, 0.0, 1.0);
            }

            col = qFog(col, vWorld);

            FragColor = vec4(col, alpha);
        }
        """;

    public const string TerrainFrag = """
        in vec3  vWorld;
        in vec3  vNormal;
        in vec2  vUv;
        in float vHeight;

        out vec4 FragColor;

        uniform vec3 uLowColor;
        uniform vec3 uHighColor;
        uniform vec3 uRockColor;
        uniform vec3 uMossColor;

        void main()
        {
            vec3 n = normalize(vNormal);
            vec3 v = normalize(uCameraPos - vWorld);

            float h = clamp(vHeight, 0.0, 1.0);
            float slope = 1.0 - clamp(n.y, 0.0, 1.0);

            // Micro relief: perturb the normal so slopes do not look like clay.
            float e = 0.45;
            float n0 = qFbm(vWorld.xz * 0.34, 2);
            float nx = qFbm((vWorld.xz + vec2(e, 0.0)) * 0.85, 3);
            float nz = qFbm((vWorld.xz + vec2(0.0, e)) * 0.85, 3);
            float n0b = qFbm(vWorld.xz * 0.065 + 21.0, 2);
            float nxb = qFbm((vWorld.xz + vec2(e * 3.0, 0.0)) * 0.13 + 21.0, 3);
            float nzb = qFbm((vWorld.xz + vec2(0.0, e * 3.0)) * 0.13 + 21.0, 3);

            float detailFade = 1.0 / (1.0 + length(uCameraPos - vWorld) * 0.012);
            float bx = -(nx - n0) * 1.10 - (nxb - n0b) * 0.85;
            float bz = -(nz - n0) * 1.10 - (nzb - n0b) * 0.85;
            n = normalize(n + vec3(bx, 0.0, bz) * detailFade);
            slope = 1.0 - clamp(n.y, 0.0, 1.0);

            vec3 col = mix(uLowColor, uHighColor, smoothstep(0.04, 0.72, h));
            col = mix(col, uRockColor, smoothstep(0.46, 0.82, slope));

            float big = qFbm(vWorld.xz * 0.045, 4);
            float mid = qFbm(vWorld.xz * 0.19 + 7.0, 3);
            float fine = qFbm(vWorld.xz * 0.90 - 3.0, 2);
            col *= 0.70 + 0.55 * big;
            col *= 0.88 + 0.24 * mid;
            col *= 0.93 + 0.14 * fine;

            // moss on gentle slopes
            float moss = smoothstep(0.45, 0.85, qFbm(vWorld.xz * 0.22 + 9.0, 3)) * (1.0 - slope);
            col = mix(col, uMossColor, moss * 0.55);

            // wet shoreline
            float shore = smoothstep(0.0, 1.6, vWorld.y);
            col = mix(col * 0.45, col, shore);

            vec3 lit = qLight(col, n, v, 0.9, 0.0, vWorld);
            lit = qFog(lit, vWorld);

            FragColor = vec4(lit, 1.0);
        }
        """;

    public const string WaterFrag = """
        in vec3  vWorld;
        in vec3  vNormal;
        in vec2  vUv;
        in float vHeight;

        out vec4 FragColor;

        uniform vec3  uDeepColor;
        uniform float uWaveScale;
        uniform float uWaveSpeed;
        uniform float uWaveStrength;

        void main()
        {
            vec2 p = vWorld.xz;

            // two travelling wave layers
            float w1 = qFbm(p * 0.018 * uWaveScale + vec2(uTime * 0.008, uTime * 0.011) * uWaveSpeed, 4);
            float w2 = qFbm(p * 0.055 * uWaveScale - vec2(uTime * 0.013, uTime * 0.007) * uWaveSpeed, 3);
            float w = (w1 - 0.5) * 0.7 + (w2 - 0.5) * 0.3;

            vec3 n = normalize(vec3(w * uWaveStrength, 1.0, w * uWaveStrength));
            vec3 v = normalize(uCameraPos - vWorld);

            vec3 r = reflect(-v, n);
            r.y = abs(r.y) * 0.55 + 0.03;

            vec3 reflection = qSky(normalize(r), 0.0);

            float fres = pow(1.0 - max(dot(n, v), 0.0), 4.0);
            fres = mix(0.18, 1.0, fres);

            vec3 deep = uDeepColor * (0.55 + 0.45 * qFbm(p * 0.03, 2));
            vec3 col = mix(deep, reflection, fres);

            // sun glints
            float glint = pow(max(dot(normalize(r), uSunDir), 0.0), 420.0);
            col += uSunColor * glint * 7.0 * uSunIntensity;

            col = qFog(col, vWorld);
            FragColor = vec4(col, 1.0);
        }
        """;

    /// <summary>
    /// Шахматный пол: бесконечные клетки, мягкие швы и глянец, ловящий небо
    /// у горизонта. Это половина «вайба» Bryce — пол уходит в перспективу.
    /// </summary>
    public const string CheckerFrag = """
        in vec3  vWorld;
        in vec3  vNormal;
        in vec2  vUv;
        in float vHeight;

        out vec4 FragColor;

        uniform vec3  uAlbedo;     // dark tiles
        uniform vec3  uTileColor;  // light tiles
        uniform float uTileSize;
        uniform float uRoughness;

        uniform vec4  uShadow0;
        uniform vec4  uShadow1;
        uniform vec4  uShadow2;
        uniform vec4  uShadow3;
        uniform vec4  uShadow4;
        uniform vec4  uShadow5;
        uniform int   uShadowCount;

        float qShadowBlob(vec3 world, vec4 sh)
        {
            float d = length(world.xz - sh.xy);
            return sh.w * (1.0 - smoothstep(0.0, sh.z, d));
        }

        void main()
        {
            vec2 p = vWorld.xz / max(uTileSize, 0.0001);
            vec2 f = fract(p);
            vec2 w = fwidth(p);

            // fade the pattern out before tiles shrink below a pixel
            float lod = smoothstep(0.05, 0.16, max(w.x, w.y));
            vec2 wc = clamp(w, vec2(0.0006), vec2(0.45));

            float sx = smoothstep(0.5 - wc.x, 0.5 + wc.x, f.x);
            float sy = smoothstep(0.5 - wc.y, 0.5 + wc.y, f.y);
            float checker = abs(sx - sy);

            // slight per-tile tone variation, like real stone
            vec2 cell = floor(p);
            float shade = 0.94 + 0.12 * qHash21(cell);

            vec3 nearTiles = mix(uAlbedo, uTileColor, checker) * shade;
            vec3 farTiles = mix(uAlbedo, uTileColor, 0.5) * 0.92;
            vec3 albedo = mix(nearTiles, farTiles, lod);

            // seams
            vec2 edge = min(f, 1.0 - f);
            float seam = smoothstep(0.0, 0.020, min(edge.x, edge.y));
            albedo *= 1.0 - (1.0 - seam) * 0.45 * (1.0 - lod);

            // soft contact shadows from objects standing on the floor
            float occ = 0.0;
            if (uShadowCount > 0) occ = max(occ, qShadowBlob(vWorld, uShadow0));
            if (uShadowCount > 1) occ = max(occ, qShadowBlob(vWorld, uShadow1));
            if (uShadowCount > 2) occ = max(occ, qShadowBlob(vWorld, uShadow2));
            if (uShadowCount > 3) occ = max(occ, qShadowBlob(vWorld, uShadow3));
            if (uShadowCount > 4) occ = max(occ, qShadowBlob(vWorld, uShadow4));
            if (uShadowCount > 5) occ = max(occ, qShadowBlob(vWorld, uShadow5));
            float shadow = 1.0 - occ;

            vec3 n = normalize(vNormal);
            if (!gl_FrontFacing) n = -n;
            vec3 v = normalize(uCameraPos - vWorld);

            vec3 col = qLight(albedo, n, v, uRoughness, 0.0, vWorld);
            col *= shadow;

            // shadowed floor picks up cool sky fill instead of turning muddy grey
            col = mix(col, col * vec3(0.80, 0.90, 1.05), (1.0 - shadow) * 0.55);

            // extra mirror sheen at grazing angles
            float ndv = clamp(dot(n, v), 0.0, 1.0);
            float fres = pow(1.0 - ndv, 5.0);
            vec3 r = reflect(-v, n);
            r.y = max(r.y, 0.015);
            vec3 sheen = qSky(normalize(r), 0.0);
            col += sheen * fres * (1.0 - uRoughness) * 0.55 * shadow;

            // cheap aerial perspective even when fog is off
            float dist = length(uCameraPos - vWorld);
            float aerial = 1.0 - exp(-dist * 0.0011);
            col = mix(col, uHorizonColor * 0.85 + uGroundColor * 0.15, aerial * 0.32);

            col = qFog(col, vWorld);
            FragColor = vec4(col, 1.0);
        }
        """;
}





