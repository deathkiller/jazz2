{
    "Version": {
        "Target": "Jazz² Resurrection"
    },

    "Animations": {
        "ENEMY_TURTLE_WALK": {
            "Path": "turtle/xmas_walk.png",
            "FrameRate": 4,
            "States": [ 0, 1, 2, 8, 9, 10 ]
        },
        "ENEMY_TURTLE_WITHDRAW": {
            "Path": "turtle/xmas_turn_start.png",
            "FrameRate": 7,
            "States": [ 1073741841 ]
        },
        "ENEMY_TURTLE_WITHDRAW_END": {
            "Path": "turtle/xmas_turn_end.png",
            "FrameRate": 7,
            "States": [ 1073741842 ]
        },
        "ENEMY_TURTLE_ATTACK": {
            "Path": "turtle/xmas_attack.png",
            "FrameRate": 7,
            "States": [ 1325400065 ]
        }
    },

    "Sounds": {
        "ENEMY_TURTLE_WITHDRAW": {
            "Paths": [ "turtle/xmas_turn_start.wav" ]
        },
        "ENEMY_TURTLE_WITHDRAW_END": {
            "Paths": [ "turtle/xmas_turn_end.wav" ]
        },
        "ENEMY_TURTLE_ATTACK": {
            "Paths": [ "turtle/xmas_attack_neck.wav" ]
        },
        "ENEMY_TURTLE_ATTACK_2": {
            "Paths": [ "turtle/xmas_attack_bite.wav" ]
        }
    },

    "Preload": [
        "Enemy/TurtleShellXmas"
    ]
}