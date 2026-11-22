"""Synthesize the pump's original mono loop from periodic motor and pipe modes."""
import math
import sys
import wave
from array import array

RATE = 24000
LENGTH = 8
samples = array("h")
for frame in range(RATE * LENGTH):
    t = frame / RATE
    angle = math.tau * t
    # All frequencies and envelopes close at the eight-second seam.
    motor = sum(amplitude * math.sin(angle * tone + phase) for tone, amplitude, phase in (
        (43, .054, .2), (86, .020, 1.7), (129, .014, 4), (172, .012, 2.1), (215, .008, .4)))
    swell = (.5 + .5 * math.sin(angle * .125 - 1.38)) ** 5
    metal = swell * sum(amplitude * math.sin(angle * tone + phase) for tone, amplitude, phase in (
        (243.375, .022, 4), (416.25, .011, 1), (513.125, .008, -1), (804.5, .005, 2)))
    samples.append(round((motor * (.85 + .15 * math.sin(angle * .375)) + metal) * 32767))
if sys.byteorder != "little":
    samples.byteswap()
with wave.open(sys.argv[1], "wb") as clip:
    clip.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
    clip.writeframes(samples.tobytes())
