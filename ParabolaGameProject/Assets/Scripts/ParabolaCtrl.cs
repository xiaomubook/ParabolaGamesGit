using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParabolaCtrl : MonoBehaviour
{
    private Vector2 mouse_Pos;  //鼠标位置的世界坐标

    public LineRenderer line1;  //轨迹线
    public int line1Num = 10;   //轨迹绘制点数
    Vector3[] points1;          //轨迹绘制点数组  

    public LineRenderer line2;  //力度线
    public float maxForce = 2;  //最大力度
    int line2Num = 2;           //线段绘制点数
    Vector3[] points2;          //绘制点数组

    Rigidbody2D rb2D;           //小球的刚体组件
    public float releaseForce;  //额外力度

    Vector2 release_Velocity;   //初速度
    float S;                    //最大水平距离
    float t;                    //水平飞行时间
    float g = 9.8f;             //重力加速度
    public Transform ground;    //地面对象
    float height;               //小球离地高度
    float xUnit = .1f;          //X轴绘制间隔

    //地面图层掩码
    public LayerMask groundLayer;

    //力度拖拽示意点物体对象
    public GameObject dragPoint;

    //轨迹线的起点颜色
    Vector4 fadeLine = new Vector4(1, 1, 1, 1);

    enum STATE
    {
        NONE = -1,

        IDLE = 0,  //静止
        GRAB,      //抓取
        DRAG,      //拖拽
        RELEASE,   //松手
        LAND,      //落地

        NUM,
    }

    private STATE state = STATE.IDLE;         //初始状态
    private STATE next_state = STATE.NONE;    //下一状态

    private void Start()
    {
        //设置Line Render的绘制点数
        line1.positionCount = line1Num;
        line2.positionCount = line2Num;

        //根据点数确定数组大小
        points1 = new Vector3[line1Num];
        points2 = new Vector3[line2Num];

        //获取刚体组件
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        //获取鼠标位置的世界坐标
        mouse_Pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //-----------------检测状态改变--------------------------
        switch (state)
        {
            //放手状态中，如果接触到地面，则进入落地状态
            case STATE.RELEASE:
                if (rb2D.IsTouchingLayers(groundLayer) && rb2D.velocity.y == 0)
                    next_state = STATE.LAND;
                break;
            //落地状态中，如果小球静止，则进入静止状态
            case STATE.LAND:
                if (rb2D.velocity == Vector2.zero)
                    next_state = STATE.IDLE;
                break;
        }

        //----------------状态初始化----------------------------
        //有状态改变才执行一次
        if(next_state != STATE.NONE)
        {
            //变换状态
            state = next_state;
            next_state = STATE.NONE;

            switch (state)
            {
                //进入抓取状态时
                case STATE.GRAB:
                    rb2D.gravityScale = 0;//关闭重力影响
                    height = transform.position.y - ground.position.y;//获取小球离地高度

                    line1.startColor = new Vector4(1, 1, 1, 1);//重新显示抛物线
                    line2.enabled = true;
                    dragPoint.SetActive(true);

                    next_state = STATE.DRAG;
                    break;
                //进入松手状态时
                case STATE.RELEASE:
                    rb2D.drag = 0;           //线性阻力
                    rb2D.gravityScale = 1;   //重力影响
                    rb2D.velocity = release_Velocity;//初速度

                    line2.enabled = false;//隐藏力度线
                    dragPoint.SetActive(false);                    
                    break;
                case STATE.LAND:
                    //对小球附加额外阻力，使其可以停下
                    rb2D.drag = 0.8f;
                    break;
            }
        }

        //-----------------状态执行--------------------------
        switch (state)
        {
            case STATE.DRAG:
                //###拖拽表示线段###

                //连接鼠标和小球位置的线段
                points2[0] = transform.position;
                points2[1] = mouse_Pos;

                //如果超过最大长度，则为最大长度
                if (Vector3.Distance(points2[0], points2[1]) > maxForce)
                {
                    points2[1] = points2[0] + (points2[1] - points2[0]).normalized * maxForce;
                }
                line2.SetPositions(points2);

                //末端小点的位置
                dragPoint.transform.position = points2[1];

                //###轨迹抛物线###
                //初速度
                release_Velocity = (points2[0] - points2[1]) * releaseForce;

                //落地的最大水平位移
                S = release_Velocity.x * (release_Velocity.y / g
                    + Mathf.Sqrt((release_Velocity.y * release_Velocity.y / g / g) + 2 * height / g));

                //根据水平位移确定轨迹的X轴绘制间隔
                xUnit = S / line1Num;

                //结合LineRender绘制图像
                for (int i = 0; i < line1Num; i++)
                {
                    points1[i].x = transform.position.x + i * xUnit;
                    points1[i].y = GetFuncPathY(points1[i].x);
                }
                line1.SetPositions(points1);
                break;
            case STATE.RELEASE:
                //轨迹线逐渐消失
                fadeLine.w -= Time.deltaTime * 2;
                Mathf.Clamp01(fadeLine.w);
                line1.startColor = fadeLine;
                break;
        }
    }

    //按下鼠标，进入抓取状态
    private void OnMouseDown()
    {
        next_state = STATE.GRAB;
    }

    //松开鼠标，进入放手状态
    private void OnMouseUp()
    {
        next_state = STATE.RELEASE;
    }

    //获取函数Y坐标，即抛物线的轨迹方程
    float GetFuncPathY(float x)
    {
        float y;
        y = (release_Velocity.y / release_Velocity.x) * (x - transform.position.x)
            - (g * (x - transform.position.x) * (x - transform.position.x)) / (2 * release_Velocity.x * release_Velocity.x)
            + transform.position.y;

        return y;
    }
}
