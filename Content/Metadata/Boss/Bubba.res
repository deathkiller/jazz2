{
    "Version": {
        "Target": "Jazz² Resurrection"
    },

    "Animations": {
        "IDLE": {
            "Path": "Bubba/hop.png",
            "States": [ 0 ],
            "FrameCount": 1
        },
        "JUMP_START": {
            "Path": "Bubba/hop.png",
            "States": [ 1073741825 ],
            "FrameCount": 4,
            "FrameRate": 11
        },
        "JUMP": {
            "Path": "Bubba/hop.png",
            "States": [ 4 ],
            "Flags": 1,
            "FrameOffset": 4,
            "FrameCount": 3,
            "FrameRate": 13
        },
        "FALL": {
            "Path": "Bubba/jump.png",
            "States": [ 8 ],
            "Flags": 1,
            "FrameCount": 3,
            "FrameRate": 12
        },
        "FALL_END": {
            "Path": "Bubba/jump.png",
            "States": [ 1073741826 ],
            "FrameOffset": 3,
            "FrameCount": 6,
            "FrameRate": 14
        },
        "SPEW_FIREBALL": {
            "Path": "Bubba/spew_fireball.png",
            "FrameCount": 12,
            "FrameRate": 12,
            "States": [ 16 ]
        },
        "SPEW_FIREBALL_END": {
            "Path": "Bubba/spew_fireball.png",
            "FrameOffset": 12,
            "FrameCount": 4,
            "FrameRate": 16,
            "States": [ 1073741828 ]
        },
        "CORPSE": {
            "Path": "Bubba/corpse.png",
            "States": [ 1073741839 ],
            "FrameRate": 5
        },

        "TORNADO_START": {
            "Path": "Bubba/tornado_start.png",
            "States": [ 1073741830 ]
        },
        "TORNADO": {
            "Path": "Bubba/tornado.png",
            "States": [ 1073741831 ]
        },
        "TORNADO_END": {
            "Path": "Bubba/tornado_end.png",
            "States": [ 1073741832 ]
        },
        "TORNADO_FALL": {
            "Path": "Bubba/jump_fall.png",
            "States": [ 1073741833 ]
        },

        "FIREBALL": {
            "Path": "Bubba/fireball.png",
            "States": [ 1073741834 ]
        }
    },

    "Sounds": {
        "JUMP": {
            "Paths": [ "Bubba/hop_1.wav", "Bubba/hop_2.wav" ]
        },
        "SNEEZE": {
            "Paths": [ "Bubba/sneeze.wav" ]
        }
    }
}