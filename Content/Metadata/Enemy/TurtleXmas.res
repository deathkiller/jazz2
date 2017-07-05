{
    "Version": {
        "Target": "Jazz² Resurrection"
    },

    "Animations": {
        "ENEMY_TURTLE_WALK": {
            "Path": "Turtle/xmas_walk.png",
            "FrameRate": 4,
            "States": [ 0, 1, 2, 8, 9, 10 ]
        },
        "ENEMY_TURTLE_WITHDRAW": {
            "Path": "Turtle/xmas_turn_start.png",
            "FrameRate": 7,
            "States": [ 1073741841 ]
        },
        "ENEMY_TURTLE_WITHDRAW_END": {
            "Path": "Turtle/xmas_turn_end.png",
            "FrameRate": 7,
            "States": [ 1073741842 ]
        },
        "ENEMY_TURTLE_ATTACK": {
            "Path": "Turtle/xmas_attack.png",
            "FrameRate": 7,
            "States": [ 1325400065 ]
        }
    },

    "Sounds": {
        "ENEMY_TURTLE_WITHDRAW": {
            "Paths": [ "Turtle/xmas_turn_start.wav" ]
        },
        "ENEMY_TURTLE_WITHDRAW_END": {
            "Paths": [ "Turtle/xmas_turn_end.wav" ]
        },
        "ENEMY_TURTLE_ATTACK": {
            "Paths": [ "Turtle/xmas_attack_neck.wav" ]
        },
        "ENEMY_TURTLE_ATTACK_2": {
            "Paths": [ "Turtle/xmas_attack_bite.wav" ]
        }
    },

    "Preload": [
        "Enemy/TurtleShellXmas"
    ]
}