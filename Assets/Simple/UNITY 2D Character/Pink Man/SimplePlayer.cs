using UnityEngine;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Windows;
using Input = UnityEngine.Input;


public class SimplePlayer : MonoBehaviour // library สำหรับตอน gameplay
{
    private Rigidbody2D rigid; // สำหรับการเคลื่อนที่
    private Animator anim; // สำหรับ animation

    [Header("Ground And Wall Check")]
    [SerializeField] private float groundDistCheck = 1f; // ระยะ sensor ที่วิ่งไปชนพื้น
    [SerializeField] private float wallDistCheck = 1f; // ระยะ sensor ที่วิ่งไปชนผนัง
    [SerializeField] private LayerMask groundLayer; // หาเฉพาะ layer ของพื้น
    public bool isGrounded = false; // ตรวจชนพื้น
    public bool isWalled = false;  // ตรวจชนกำแพง

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    private float X_input;
    private float Y_input;
    //private int facing = 1;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f; // แรงกระโดด
    private bool isJumping = false;
    private bool isWallJumping = false;
    private bool isWallSliding = false;
    private bool canDoubleJump = false; // DoubleJump ได้แค่ครั้งเดียว
    public int facing = 1; // wall jump

    [SerializeField] private float coyoteTimeLimit = .5f; // ระยะเวลากระโดดกลางอากาศ
    [SerializeField] private float bufferTimeLimit = .5f; // ระยะเวลาที่สามารถกดเพื่อกระโดดก่อนถึงพื้นได้
    private float coyoteTime = -10000f; // เวลาที่ยอมให้กดกระโดดกลางอากาศได้
    private float bufferTime = -10000f; // เวลาที่ยอมให้กดกระโดดกลางอากาศได้

    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f);


    private void Awake() // ทำงานก่อนเข้ามาใน game
    {
        rigid = GetComponent<Rigidbody2D>(); // มันอยู่ที่ gameobject นี้
        anim = GetComponentInChildren<Animator>(); // ใช้ InChildren เพราะ Animator อยู่ที่ลูก
    }
    private void Update() // ทำงานทุก frame
    {
        JumpState(); // ตรวจสถานะว่า อยู่บนพื้น กำลังกระโดด กำลังลงพื้น หรือ wallSlide
        Jump(); // สั่งกระโดดในแบบต่างๆ
        WallSlide(); // สั่ง wallSlide
        InputVal(); // ตรวจ input จากผู้เล่น
        Move(); // สั้งเคลื่อนไหวทั้งบนพื้นและอากาศ
        Flip(); // สั่งหันหน้าไปทางทิศการเคลื่อนที่อัดโนมัต
        GroundAndWallCheck(); // ตรวจจับพื้นและผนัง
        Animation(); // สั่ง animation
    }
    private void JumpState()
    {
        if (!isGrounded && !isJumping) // takeoff
        {
            isJumping = true;

            if (rigid.linearVelocityY <= 0f) // Fall หล่นอยู่
            {
                coyoteTime = Time.time;
            }
        }

        if (isGrounded && isJumping) // landing
        {
            isJumping = false;
            isWallJumping = false;
            isWallSliding = false;
            canDoubleJump = false;
        }

        if (isWalled) // ตรวจ wallSlide
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;

            if (isGrounded)
            {
                isWallSliding = false;
            }
            else
            {
                isWallSliding = true;
            }
        }
        else // ยกเลิก wallSlide
        {
            isWallSliding = false;
        }
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isWalled)
            {
                if (isGrounded) // *** normalJump
                {
                    canDoubleJump = true;
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                }
                else // doubleJump, coyoteJump
                {
                    if (rigid.linearVelocityY > 0f && canDoubleJump) // *** doubleJump
                    {
                        canDoubleJump = false;
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                    }

                    if (rigid.linearVelocityY <= 0f)
                    {
                        if (Time.time < coyoteTime + coyoteTimeLimit) // *** coyoteJump
                        {
                            coyoteTime = 0f;
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                        }
                        else // เริ่มนับ bufferJump
                        {
                            bufferTime = Time.time;
                        }
                    }
                }
            }
            else // *** wallJump
            {
                canDoubleJump = false;
                isWallJumping = true;
                rigid.linearVelocity = new Vector2(wallJumpForce.x * facing, wallJumpForce.y);
            }
        }
        else // *** bufferJump
        {
            if (isGrounded && Time.time < bufferTime + bufferTimeLimit)
            {
                bufferTime = 0f;
                rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
            }
        }
    }
    private void WallSlide()
    {
        if (!isWalled || isGrounded || isWallJumping || rigid.linearVelocityY > 0f)
            return;

        float Y_slide = Y_input < 0f ? 1f : .5f;
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * Y_slide);
    }
    private void InputVal()
    {

    }
    private void Move()
    {
        if (isWallJumping || isWallSliding)
            return;

        if (isGrounded)
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY);
        }
        else
        {
            float X_airMove = X_input != 0f ? X_input * moveSpeed : rigid.linearVelocityX;
            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }
    private void Flip() // หมนุตัวละคร ตามทิศการเคลื่อนที่
    {
        if (rigid.linearVelocityX > 0.1f) // ถ้าผลักไปทางขวามือ
        {
            facing = -1; // หันหน้าตรงข้าม
            transform.rotation = Quaternion.identity; // หันตอนเริ่ม
        }
        else if (rigid.linearVelocityX < -0.1f)  //  ถ้าผลักไม่ทางขวามือ
        {
            facing = 1; // หันหน้าตรงข้าม
            transform.rotation = Quaternion.Euler(0f, 180f, 0f); // หันไป 180 องศา
        }
    }
    private void GroundAndWallCheck()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundDistCheck, groundLayer); // sensor พื้น
        isWalled = Physics2D.Raycast(transform.position, transform.right, wallDistCheck, groundLayer); // sensor ผนัง
    }
    private void OnDrawGizmos() // กราฟฟิกแสดงผลของ sensor ตรวจจับพื้นและผนัง
    {
        Gizmos.color = Color.blue; // เส้นสีน้ำเงิน
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistCheck); // เส้น sensor ตรวจพื้น
        Gizmos.color = Color.red; // เส้นแดง
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallDistCheck); // เส้น sensor ตรวจผนัง
    }
    private void Animation()
    {

    }
}
