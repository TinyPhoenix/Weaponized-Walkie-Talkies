﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    [Header(" --- EFFECTS --- ")]
    public Effect damaged;
    public Effect jump;
    
    [Header(" --- OTHER STUFF --- ")]
    public PlayerAnimator playerAnimator;
    public WalkieController activelyHeldWalkie;
    public Transform walkieAnchor;

    public float move_speed = 10f; // units per second
    public float rotate_speed = 10f; // degrees per second
    [HideInInspector]
    public InputDevice inputDevice;
    public Rigidbody rigidBody;
    public float rotationDeadzone = 0.001f;

    public bool altAntennaControl = false;
    public float antennaSpeed = 10;
    public AnimationCurve antennaSpeedCurve;

    public InputDevice.GenericInputs jumpAxis = InputDevice.GenericInputs.ACTION_1;

    //Input
    private Dictionary<string, InputDevice.GenericInputs> keyboardInputBindings = new Dictionary<string, InputDevice.GenericInputs>() {
        {"inputMoveX", InputDevice.GenericInputs.AXIS_1_X}, // ad
        {"inputMoveY", InputDevice.GenericInputs.AXIS_1_Y}, // ws
        {"inputLookX", InputDevice.GenericInputs.AXIS_2_X}, // left right
        {"inputLookY", InputDevice.GenericInputs.AXIS_2_X}, // up down
        {"inputJump", InputDevice.GenericInputs.ACTION_1}, // space
        {"inputToggleWalkieOn", InputDevice.GenericInputs.ACTION_4}, // alt
        {"inputAntennaIn", InputDevice.GenericInputs.ACTION_3}, // e
        {"inputAntennaOut", InputDevice.GenericInputs.ACTION_2} // q
    };

    private Dictionary<string, InputDevice.GenericInputs> xboxInputBindings = new Dictionary<string, InputDevice.GenericInputs>() {
        {"inputMoveX", InputDevice.GenericInputs.AXIS_1_X}, // left_joy_x
        {"inputMoveY", InputDevice.GenericInputs.AXIS_1_Y}, // left_joy_y
        {"inputLookX", InputDevice.GenericInputs.AXIS_2_X}, // right_joy_x
        {"inputLookY", InputDevice.GenericInputs.AXIS_2_X}, // right_joy_y
        {"inputJump", InputDevice.GenericInputs.ACTION_1}, // A
        {"inputToggleWalkieOn", InputDevice.GenericInputs.ACTION_4}, // Y
        {"inputAntennaIn", InputDevice.GenericInputs.AXIS_ALT_1}, // left trigger
        {"inputAntennaOut", InputDevice.GenericInputs.AXIS_ALT_2} // right trigger
    };

    private Dictionary<string, InputDevice.GenericInputs> finalInputBindings;

    private Vector2 inputMove = Vector2.zero;
    private Vector2 inputLook = Vector2.zero;
    private float inputJump;
    private float inputToggleWalkieOn;
    private float inputPreviousToggleWalkieOn;
    private float inputAntennaOut;
    private float inputAntennaIn;

    private bool jumpLock = false;
    private float lastJumpTime = 0f;
    [SerializeField]
    private bool grounded = false;
    public float jumpLockLength = 1f;
    public float jumpForce = 7f;
    public string jumpMaskString = "Everything";
    private float distanceToGround = .05f;
    private LayerMask jumpMask;
    public Vector3 jumpDirection = Vector3.up;
    private bool dead = false;

    [SerializeField]
    private float maxHealth = 250f;
    [SerializeField]
    private float health = 250f;

    public WalkieController.Team playerTeam = WalkieController.Team.RED;
    public Collider feetCollider;

    public bool Dead
    {
        get
        {
            return dead;
        }

        set
        {
            dead = value;

            //gameObject.SetActive(!dead);

            foreach(SkinnedMeshRenderer mesh in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                mesh.enabled = !dead;
            }

            //foreach(Collider collider in GetComponentsInChildren<Collider>())
            //{
            //    collider.enabled = !dead;
            //}
        }
    }

    // Use this for initialization
    void Start()
    {
        jumpMask = LayerMask.NameToLayer(jumpMaskString);
        if (jump != null)
        {
            jump = Instantiate(jump);
        }
        if (damaged != null)
        {
            damaged = Instantiate(damaged);
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (!Dead)
        {
            UpdateWalkiePosition();

            if (inputDevice != null)
            {
                UpdateInput();

                UpdateMovement();
                UpdateWalkieAntenna();
            }

            UpdateJump();
        }
        else
        {
            if(activelyHeldWalkie != null)
            {
                activelyHeldWalkie.power = activelyHeldWalkie.maxPower;
                activelyHeldWalkie.on = true;
            }
        }

        if(health <= 0)
        {
            Dead = true;
        }
    }

    public void UpdateWalkiePosition()
    {
        if (activelyHeldWalkie != null)
        {
            activelyHeldWalkie.transform.position = walkieAnchor.transform.position;
            activelyHeldWalkie.transform.rotation = walkieAnchor.transform.rotation;

            Rigidbody walkieRigidBody = activelyHeldWalkie.GetComponent<Rigidbody>();
            if (walkieRigidBody != null)
            {
                walkieRigidBody.velocity = Vector3.zero;
            }
        }
    }

    private void UpdateInput()
    {
        inputPreviousToggleWalkieOn = inputToggleWalkieOn;

        Vector2 keyboardMoveMult = new Vector2(1, 1);
        Vector2 xboxMoveMult = new Vector2(1, -1); // xbox gives the y-axis for movement negated
        Vector2 finalMoveMult = xboxMoveMult;

        Vector2 keyboardLookMult = new Vector2(1, -1); // keyboard gives the y-axis for looking negated
        Vector2 xboxLookMult = new Vector2(1, 1);
        Vector2 finalLookMult = xboxLookMult;

        finalInputBindings = xboxInputBindings;

        if (inputDevice.Type == InputDevice.InputDeviceType.KEYBOARD)
        {
            finalMoveMult = keyboardMoveMult;
            finalLookMult = keyboardLookMult;
            finalInputBindings = keyboardInputBindings;
        } 

        inputMove = new Vector2(inputDevice.GetAxis(finalInputBindings["inputMoveX"]) * finalMoveMult.x, inputDevice.GetAxis(finalInputBindings["inputMoveY"]) * finalMoveMult.y);
        inputLook = new Vector2(inputDevice.GetAxis(finalInputBindings["inputLookX"]) * finalLookMult.x, inputDevice.GetAxis(finalInputBindings["inputLookY"]) * finalLookMult.y);
        inputJump = inputDevice.GetAxis(finalInputBindings["inputJump"]);
        inputToggleWalkieOn = inputDevice.GetAxis(finalInputBindings["inputToggleWalkieOn"]);
        inputAntennaIn = inputDevice.GetAxis(finalInputBindings["inputAntennaIn"]);
        inputAntennaOut = inputDevice.GetAxis(finalInputBindings["inputAntennaOut"]);
    }

    private void UpdateMovement()
    {
        rigidBody.AddForce(new Vector3(inputMove.x, 0f, inputMove.y) * Time.deltaTime * move_speed);

        Vector3 goalRotationVector = Vector3.zero;

        if (inputLook.magnitude > .1f)
        {
            goalRotationVector = new Vector3(inputLook.x, 0f, -inputLook.y);
        }
        else if (inputMove.magnitude > .1f)
        {
            goalRotationVector = new Vector3(inputMove.x, 0f, inputMove.y);
        }

        Quaternion goalRotation = new Quaternion();
        if (goalRotationVector != Vector3.zero)
        {
            goalRotation = Quaternion.LookRotation(goalRotationVector);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, goalRotation, Time.deltaTime * move_speed);
        }

        playerAnimator.RunBlend = inputMove.magnitude;
        playerAnimator.SpeedMultiplier = inputMove.magnitude;
    }

    private void UpdateWalkieAntenna()
    {
        if (activelyHeldWalkie != null)
        {
            if (altAntennaControl)
            {
                float newAntennaLenngth = activelyHeldWalkie.AntennaLength +
                    ((antennaSpeedCurve.Evaluate(inputAntennaOut) - antennaSpeedCurve.Evaluate(inputAntennaIn)) * Time.deltaTime * antennaSpeed);
                activelyHeldWalkie.AntennaLength = Mathf.Clamp(newAntennaLenngth, 0, 1);
            }
            else
            {
                activelyHeldWalkie.AntennaLength = inputLook.magnitude;
            }

            if (inputToggleWalkieOn > 0 && inputPreviousToggleWalkieOn == 0)
            {
                activelyHeldWalkie.on = !activelyHeldWalkie.on;
            }
        }
    }

    public float getHealth()
    {
        return health;
    }

    public float getMaxHealth()
    {
        return maxHealth;
    }

    public void setMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
    }

    public void changeHealth(float amount) // negative or positive
    {
        if (health + amount > maxHealth)
        {
            health = maxHealth;
        }
        else if (health + amount < 0)
        {
            health = 0;
        }
        else
        {
            health += amount;
        }

        if (amount < 0 && health > 0)
        {
            if (damaged != null && !damaged.IsPlaying)
            {
                damaged.Play();
            }
        }
    }

    public bool Grounded
    {
        get
        {
            return grounded;
        }

        set
        {
            grounded = value;
        }
    }

    private void UpdateJump()
    {

        float jump_axis = 0;
        if (inputDevice != null)
            jump_axis = inputDevice.GetAxis(jumpAxis);

        bool newGrounded = CheckIfGrounded();

        if (jumpLock && Time.time - lastJumpTime > jumpLockLength && jump_axis == 0)
        {
            jumpLock = false;
        }

        if (!jumpLock)
        {
            if (newGrounded && !Grounded)
            {
                jump_axis = 0;
            }

            Grounded = newGrounded;
        }

        if (jump_axis > 0)
        {
            Jump();
        }
    }

    public void ResetHealth()
    {
        health = maxHealth;
        Dead = false;
    }

    private void Jump()
    {
        if (Grounded && !jumpLock)
        {
            Vector3 jumpVelocity = jumpDirection * (jumpForce);

            rigidBody.velocity += jumpVelocity;
            //animationController.TriggerJump = true;
            Grounded = false;
            jumpLock = true;
            lastJumpTime = Time.time;

            //jumpEffect.transform.position = transform.position;
            if (jump != null)
            {
                jump.Play();
            }
        }
    }

    /// <summary>
    /// Determins wheather the player collider is on the ground.
    /// </summary>
    private bool CheckIfGrounded()
    {
        bool groundCheck = false;
        Collider collider = collider = this.GetComponent<Collider>();

        if(feetCollider != null)
        {
            collider = feetCollider;
        }


        RaycastHit raycastHit = new RaycastHit();
        groundCheck = Physics.Raycast(
            new Ray(collider.bounds.center, Vector3.down),
            out raycastHit,
            collider.bounds.size.y / 2 + distanceToGround,
            jumpMask);

        return groundCheck;
    }
}
