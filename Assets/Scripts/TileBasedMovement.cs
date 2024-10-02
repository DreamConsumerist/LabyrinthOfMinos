using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TileBasedMovement : MonoBehaviour
{
    Quaternion up = new Quaternion(1, 1, 1, 1);
    Quaternion right = new Quaternion(1, 1, 1, 1);
    Quaternion down = new Quaternion(1, 1, 1, 1);
    Quaternion left = new Quaternion(1, 1, 1, 1);

    Quaternion currentDirection = new Quaternion(1, 1, 1, 1);
    Quaternion lookDirection = new Quaternion(1, 1, 1, 1);

    Vector3 nextPos, destination, direction, bounceDest, bounceOrigin;

    bool bounce1 = false;
    bool bounce2 = false;
    //bool goLookY = false;
    //bool goLookX = false;

    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float turnSpeed = 10f;
    [SerializeField] float bounceSpeed = 10f;
    [SerializeField] float interval = 10f;
    [SerializeField] float bounceLength = 2f;
    [SerializeField] float povHeight = 10f;
    //[SerializeField] float lookSpeed = 10f;

    private void Start()
    {
        up = Quaternion.Euler(povHeight, 0, 0);
        right = Quaternion.Euler(povHeight, 90, 0);
        left = Quaternion.Euler(povHeight, -90, 0);
        down = Quaternion.Euler(povHeight, 180, 0);
        currentDirection = up;
        lookDirection = currentDirection;
        nextPos = Vector3.forward * interval;
        destination = transform.position;
    }

    void Update()
    {
        //if ((transform.position == destination) && (transform.rotation == currentDirection))
        //{
        //    if ((!goLookX) && (!goLookY))
        //    {
        //        lookDirection = currentDirection;
        //    }
        //    LookAround();
        //}

        if (bounce1 || bounce2)
        {
            BounceMove();
        }
        else
        {
            if ((Input.GetKey(KeyCode.LeftShift)) || (Input.GetKey(KeyCode.RightShift)))
            {
                ShiftMove();
            }
            else
            {

                Move();

            }
        }
    }
    void Move()
    {
        UpdateTransform();

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if ((currentDirection == up) && (ValidMove(Vector3.forward * interval)))
            {
                nextPos = Vector3.forward * interval;
                SetDestination();
            }
            else if ((currentDirection == left) && (ValidMove(Vector3.left * interval)))
            {
                nextPos = Vector3.left * interval;
                SetDestination();
            }
            else if ((currentDirection == right) && (ValidMove(Vector3.right * interval)))
            {
                nextPos = Vector3.right * interval;
                SetDestination();
            }
            else if ((currentDirection == down) && (ValidMove(Vector3.back * interval)))
            {
                nextPos = Vector3.back * interval;
                SetDestination();
            }
            else
            {
                BounceInitForward();
            }
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if ((currentDirection == up) && ValidMove(Vector3.back * interval))
            {
                nextPos = Vector3.back * interval;
                SetDestination();
            }
            else if ((currentDirection == left) && ValidMove(Vector3.right * interval))
            {
                nextPos = Vector3.right * interval;
                SetDestination();
            }
            else if ((currentDirection == right) && ValidMove(Vector3.left * interval))
            {
                nextPos = Vector3.left * interval;
                SetDestination();
            }
            else if ((currentDirection == down) && ValidMove(Vector3.forward * interval))
            {
                nextPos = Vector3.forward * interval;
                SetDestination();
            }
            else
            {
                BounceInitBackward();
            }
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentDirection == up)
            {
                currentDirection = left;
            }
            else if (currentDirection == left)
            {
                currentDirection = down;
            }
            else if (currentDirection == right)
            {
                currentDirection = up;
            }
            else if (currentDirection == down)
            {
                currentDirection = right;
            }
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentDirection == up)
            {
                currentDirection = right;
            }
            else if (currentDirection == left)
            {
                currentDirection = up;
            }
            else if (currentDirection == right)
            {
                currentDirection = down;
            }
            else if (currentDirection == down)
            {
                currentDirection = left;
            }
        }
    }

    void ShiftMove()
    {
        UpdateTransform();

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if ((currentDirection == up) && (ValidMove(Vector3.forward * interval)))
            {
                nextPos = Vector3.forward * interval;
                SetDestination();
            }
            else if ((currentDirection == left) && (ValidMove(Vector3.left * interval)))
            {
                nextPos = Vector3.left * interval;
                SetDestination();
            }
            else if ((currentDirection == right) && (ValidMove(Vector3.right * interval)))
            {
                nextPos = Vector3.right * interval;
                SetDestination();
            }
            else if ((currentDirection == down) && (ValidMove(Vector3.back * interval)))
            {
                nextPos = Vector3.back * interval;
                SetDestination();
            }
            else
            {
                BounceInitForward();
            }
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if ((currentDirection == up) && ValidMove(Vector3.back * interval))
            {
                nextPos = Vector3.back * interval;
                SetDestination();
            }
            else if ((currentDirection == left) && ValidMove(Vector3.right * interval))
            {
                nextPos = Vector3.right * interval;
                SetDestination();
            }
            else if ((currentDirection == right) && ValidMove(Vector3.left * interval))
            {
                nextPos = Vector3.left * interval;
                SetDestination();
            }
            else if ((currentDirection == down) && ValidMove(Vector3.forward * interval))
            {
                nextPos = Vector3.forward * interval;
                SetDestination();
            }
            else
            {
                BounceInitBackward();
            }
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if ((currentDirection == up) && ValidMove(Vector3.left * interval))
            {
                nextPos = Vector3.left * interval;
                SetDestination();
            }
            else if ((currentDirection == left) && ValidMove(Vector3.back * interval))
            {
                nextPos = Vector3.back * interval;
                SetDestination();
            }
            else if ((currentDirection == right) && ValidMove(Vector3.forward * interval))
            {
                nextPos = Vector3.forward * interval;
                SetDestination();
            }
            else if ((currentDirection == down) && ValidMove(Vector3.right * interval))
            {
                nextPos = Vector3.right * interval;
                SetDestination();
            }
            else
            {
                BounceInitLeft();
            }
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if ((currentDirection == up) && ValidMove(Vector3.right * interval))
            {
                nextPos = Vector3.right * interval;
                SetDestination();
            }
            else if ((currentDirection == left) && ValidMove(Vector3.forward * interval))
            {
                nextPos = Vector3.forward * interval;
                SetDestination();
            }
            else if ((currentDirection == right) && ValidMove(Vector3.back * interval))
            {
                nextPos = Vector3.back * interval;
                SetDestination();
            }
            else if ((currentDirection == down) && ValidMove(Vector3.left * interval))
            {
                nextPos = Vector3.left * interval;
                SetDestination();
            }
            else
            {
                BounceInitRight();
            }
        }
    }

    void SetDestination()
    {
        destination = transform.position + nextPos;

        destination.x = 10 * (int)Math.Round(destination.x / 10.0);
        destination.z = 10 * (int)Math.Round(destination.z / 10.0);
    }

    bool ValidMove(Vector3 check)
    {
        Ray myRay = new Ray(transform.position, check);
        RaycastHit hit;

        if (Physics.Raycast(myRay, out hit, 10))
        {
            if (hit.collider.tag == "Wall")
            {
                return false;
            }
        }
        return true;
    }

    void BounceInitForward()
    {
        bounce1 = true;
        bounceOrigin = transform.position;
        if (currentDirection == up)
        {
            nextPos = Vector3.forward * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == down)
        {
            nextPos = Vector3.back * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == left)
        {
            nextPos = Vector3.left * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == right)
        {
            nextPos = Vector3.right * bounceLength;
            bounceDest = transform.position + nextPos;
        }
    }

    void BounceInitBackward()
    {
        bounce1 = true;
        bounceOrigin = transform.position;
        if (currentDirection == up)
        {
            nextPos = Vector3.back * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == down)
        {
            nextPos = Vector3.up * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == left)
        {
            nextPos = Vector3.right * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == right)
        {
            nextPos = Vector3.left * bounceLength;
            bounceDest = transform.position + nextPos;
        }
    }
    void BounceInitLeft()
    {
        bounce1 = true;
        bounceOrigin = transform.position;
        if (currentDirection == up)
        {
            nextPos = Vector3.left * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == down)
        {
            nextPos = Vector3.right * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == left)
        {
            nextPos = Vector3.back * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == right)
        {
            nextPos = Vector3.forward * bounceLength;
            bounceDest = transform.position + nextPos;
        }
    }
    void BounceInitRight()
    {
        bounce1 = true;
        bounceOrigin = transform.position;
        if (currentDirection == up)
        {
            nextPos = Vector3.right * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == down)
        {
            nextPos = Vector3.left * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == left)
        {
            nextPos = Vector3.forward * bounceLength;
            bounceDest = transform.position + nextPos;
        }
        else if (currentDirection == right)
        {
            nextPos = Vector3.back * bounceLength;
            bounceDest = transform.position + nextPos;
        }
    }

    void BounceMove()
    {
        if (bounce1)
        {
            transform.position = Vector3.Lerp(transform.position, bounceDest, bounceSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, bounceDest) < 0.01f)
            {
                transform.position = bounceDest;
                bounce1 = false;
                bounce2 = true;
            }
        }
        else if (bounce2)
        {
            transform.position = Vector3.Lerp(transform.position, bounceOrigin, bounceSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, bounceOrigin) < 0.01f)
            {
                transform.position = bounceOrigin;
                bounce2 = false;
            }
        }
    }

    void UpdateTransform()
    {
        transform.position = Vector3.Lerp(transform.position, destination, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) < 0.01f)
        {
            transform.position = destination;
        }
        transform.rotation = Quaternion.Lerp(transform.rotation, currentDirection, turnSpeed * Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, currentDirection) < .1f)
        {
            transform.rotation = currentDirection;
        }
    } //Known issue is that Move and ShiftMove share update method, thus the destination can immediately be replaced before movement is complete, causing some diagonal lerp.

    //void LookAround() (Will return later perhaps, moving on for now)
    //{
    //    if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)))
    //    {
    //        goLookY = true;

    //    }
    //    else
    //    {
    //        goLookY = false;
    //    }
    //    if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)))
    //    {
    //        goLookX = true;

    //    }
    //    else
    //    {
    //        goLookX = false;
    //    }

    //    if (!goLookY)
    //    {
    //        lookDirection.y = currentDirection.y;
    //    }
    //    else
    //    {
    //        if (Input.GetKey(KeyCode.UpArrow))
    //        {
    //            lookDirection.y = currentDirection.y 
    //        }
    //    }

    //}
}

