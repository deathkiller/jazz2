{
    "Version": {
        "Target": "Jazz² Resurrection"
    },

    "Animations": {
        "ENEMY_TURTLE_WALK": {
            "Path": "turtle/walk.png",
            "FrameRate": 4,
            "States": [ 0, 1, 2, 8, 9, 10 ]
        },
        "ENEMY_TURTLE_WITHDRAW": {
            "Path": "turtle/turn_start.png",
            "FrameRate": 7,
            "States": [ 1073741841 ]
        },
        "ENEMY_TURTLE_WITHDRAW_END": {
            "Path": "turtle/turn_end.png",
            "FrameRate": 7,
            "States": [ 1073741842 ]
        },
        "ENEMY_TURTLE_ATTACK": {
            "Path": "turtle/attack.png",
            "FrameRate": 7,
            "States": [ 1325400065 ]
        }
    },

    "Sounds": {
        "ENEMY_TURTLE_WITHDRAW": {
            "Paths": [ "turtle/turn_start.wav" ]
        },
        "ENEMY_TURTLE_WITHDRAW_END": {
            "Paths": [ "turtle/turn_end.wav" ]
        },
        "ENEMY_TURTLE_ATTACK": {
            "Paths": [ "turtle/attack_neck.wav" ]
        },
        "ENEMY_TURTLE_ATTACK_2": {
            "Paths": [ "turtle/attack_bite.wav" ]
        }
    },

    "Preload": [
        "Enemy/TurtleShell"
    ]
}