#!/usr/bin/env python3
"""Sintetiza los sonidos ambiente de la plaza (sin muestras externas).

Genera en Assets/_Guadalupe/Audio/Ambience/:
  - AMB_Fountain.wav  chorro suave cayendo en el pilón (bucle de 12 s)
  - AMB_Birds.wav     piar de gorriones y vencejos sobre un fondo de brisa (bucle de 20 s)
  - SFX_Bell.wav      una campanada de campana grande de bronce (8 s)

Uso: python3 Tools/Audio/sintetizar_ambiente.py [fuente|pajaros|campana ...]
"""
import array
import math
import os
import random
import wave

RATE = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Guadalupe", "Audio", "Ambience")


def save(name, samples, gain=0.9):
    peak = max(1e-9, max(abs(s) for s in samples))
    data = array.array("h", (int(max(-1.0, min(1.0, s / peak * gain)) * 32767) for s in samples))
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print("ok", name, len(samples) / RATE, "s")


def loop(samples, fade_seconds=1.0):
    """Funde el final con el principio para que el bucle no tenga corte."""
    n = int(fade_seconds * RATE)
    body = samples[: len(samples) - n]
    for i in range(n):
        t = i / n
        body[i] = body[i] * t + samples[len(samples) - n + i] * (1 - t)
    return body


class Biquad:
    """Filtro paso banda (RBJ) de ganancia constante en el pico."""

    def __init__(self, freq, q):
        w = 2 * math.pi * freq / RATE
        alpha = math.sin(w) / (2 * q)
        a0 = 1 + alpha
        self.b0, self.b2 = alpha / a0, -alpha / a0
        self.a1, self.a2 = -2 * math.cos(w) / a0, (1 - alpha) / a0
        self.x1 = self.x2 = self.y1 = self.y2 = 0.0

    def __call__(self, x):
        y = self.b0 * x + self.b2 * self.x2 - self.a1 * self.y1 - self.a2 * self.y2
        self.x2, self.x1, self.y2, self.y1 = self.x1, x, self.y1, y
        return y


def fountain(seconds=13):
    """Chorro suave: rumor grave de agua (sin el siseo de agudos) que sube y baja despacio, con gotas sueltas."""
    rnd = random.Random(1)
    n = seconds * RATE
    bands = [Biquad(f, 0.8) for f in (220, 450, 850, 1500)]
    weights = (0.9, 1.0, 0.55, 0.2)
    out = []
    for i in range(n):
        x = rnd.uniform(-1, 1)
        swell = 0.8 + 0.2 * math.sin(i / RATE * 2 * math.pi * 0.23) * math.sin(i / RATE * 2 * math.pi * 0.71)
        out.append(sum(wt * b(x) for wt, b in zip(weights, bands)) * swell)

    # Gotas: tonos cortos que caen de 900 a 400 Hz y se apagan en unos 60 ms.
    level = max(abs(s) for s in out)
    t0 = 0
    while t0 < n:
        t0 += int(rnd.uniform(0.08, 0.35) * RATE)
        f0 = rnd.uniform(500, 900)
        amp = rnd.uniform(0.15, 0.35) * level
        phase = 0.0
        for k in range(int(0.12 * RATE)):
            if t0 + k >= n:
                break
            tt = k / RATE
            phase += 2 * math.pi * f0 * (1 - 0.5 * min(1.0, tt / 0.05)) / RATE
            out[t0 + k] += amp * math.exp(-tt / 0.02) * math.sin(phase)
    return loop(out)


def birds(seconds=21):
    rnd = random.Random(2)
    n = seconds * RATE
    out = [0.0] * n
    breeze = Biquad(400, 0.5)
    for i in range(n):
        out[i] = breeze(rnd.uniform(-1, 1)) * 0.05 * (0.6 + 0.4 * math.sin(i / RATE * 0.5))
    t = 0.3
    while t < seconds - 1.5:
        # Una frase de 2 a 6 notas de un mismo pájaro.
        base = rnd.uniform(2600, 4800)
        amp = rnd.uniform(0.15, 0.45)
        for _ in range(rnd.randint(2, 6)):
            dur = rnd.uniform(0.04, 0.12)
            sweep = rnd.uniform(-1500, 1500)
            start = int(t * RATE)
            phase = 0.0
            for k in range(int(dur * RATE)):
                u = k / (dur * RATE)
                f = base + sweep * u + 300 * math.sin(2 * math.pi * 40 * k / RATE)
                phase += 2 * math.pi * f / RATE
                env = math.sin(math.pi * u) ** 2
                if start + k < n:
                    out[start + k] += amp * env * math.sin(phase)
            t += dur + rnd.uniform(0.03, 0.12)
        t += rnd.uniform(0.4, 2.2)
    return loop(out)


def bell(seconds=8, nominal=196.0):
    # Parciales de una campana de bronce afinada (hum, prime, tercera menor, quinta, nominal y superiores).
    partials = [(0.5, 0.35, 6.0), (1.0, 0.6, 4.0), (1.2, 0.45, 3.0), (1.5, 0.25, 2.5), (2.0, 0.8, 2.2),
                (2.5, 0.3, 1.5), (3.0, 0.25, 1.2), (4.2, 0.15, 0.8), (5.4, 0.08, 0.5)]
    n = seconds * RATE
    out = []
    for i in range(n):
        t = i / RATE
        strike = math.exp(-t * 30) * 0.4 * math.sin(2 * math.pi * nominal * 7.3 * t)
        s = strike
        for ratio, amp, decay in partials:
            f = nominal * ratio
            s += amp * math.exp(-t / decay) * math.sin(2 * math.pi * f * t + ratio)
        out.append(s)
    return out


if __name__ == "__main__":
    import sys

    # Con nombres como argumentos (p. ej. «fuente») solo se regeneran esos sonidos.
    wanted = set(sys.argv[1:])
    sounds = (
        ("fuente", "AMB_Fountain", fountain, 0.5),
        ("pajaros", "AMB_Birds", birds, 0.7),
        ("campana", "SFX_Bell", bell, 0.9),
    )
    for name, file_name, make, gain in sounds:
        if not wanted or name in wanted:
            save(file_name + ".wav", make(), gain)
