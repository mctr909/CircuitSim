#define DELTA_TIME 0.01f

// https://www.sports-sensing.com/brands/labss/motionmeasurement/motion_biomechanics/quaternion01.html
// https://www.sports-sensing.com/brands/labss/motionmeasurement/motion_biomechanics/rodrigues_formula.html
// https://www.sports-sensing.com/brands/labss/motionmeasurement/motion_biomechanics/quaternion02.html
// https://www.sports-sensing.com/brands/labss/motionmeasurement/motion_biomechanics/quaternion03.html

// 姿勢(q)
float qx = 0.0f, qy = 0.0f, qz = 0.0f, qw = 1.0f;
// 補正勾配の反映量
float beta = 0.01f;

void update() {
	// 角速度(ω)
	float wx, wy, wz;
	// 角速度(-32768～32768)を(-1～1)に変換
	wx = 3.0517578125e-5f;
	wy = 3.0517578125e-5f;
	wz = 3.0517578125e-5f;
	// 回転量(Δq)
	float dqx, dqy, dqz, dqw;
	// 回転量(Δq)＝姿勢(q)が角速度(w)で回転するときの時間変化
	// |  0 -wx -wy -wz || qx |
	// | wx   0  wz -wy || qy |
	// | wy -wz   0  wx || qz |
	// | wz  wy -wx   0 || qw |
	dqx = 0.5f*(      -wx*qy -wy*qz -wz*qw);
	dqy = 0.5f*(wx*qx        +wz*qz -wy*qw);
	dqz = 0.5f*(wy*qx -wz*qy        +wx*qw);
	dqw = 0.5f*(wz*qx +wy*qy -wx*qz       );
	if(!(0 == ax && 0 == ay && 0 == az)) {
		// 重力の向き(g)
		float gx, gy, gz;
		// 方位角(b)
		float bx, by, bz;
		// 磁界の向きの変化量(Δm)
		float dmx, dmy, dmz;
		{
			float qxqx = qx*qx;
			float qxqy = qx*qy;
			float qxqz = qx*qz;
			float qxqw = qx*qw;
			float qyqy = qy*qy;
			float qyqz = qy*qz;
			float qyqw = qy*qw;
			float qzqz = qz*qz;
			float qzqw = qz*qw;
			float qwqw = qw*qw;
			float k;
			// 加速度(a)
			float ax, ay, az;
			// 加速度を正規化
			k = 1.0f / sqrtf(ax*ax + ay*ay + az*az);
			ax *= k, ay *= k, az *= k;
			// 重力の向き(g)
			gx =        2.0f*(qyqw-qxqz) - ax;
			gy =        2.0f*(qzqw+qxqy) - ay;
			gz = 1.0f - 2.0f*(qyqy-qzqz) - az;
			// 磁界の向き(m)
			float mx, my, mz;
			// 磁界の向きを正規化
			k = 1.0f / sqrtf(mx*mx + my*my + mz*mz);
			mx *= k, my *= k, mz *= k;
			float _2mx = 2.0f*mx;
			float _2my = 2.0f*my;
			float _2mz = 2.0f*mz;
			// 方位角(b)＝磁界の向き(m)を姿勢(q)で回転させた向き
			// | 2(qxqx+qyqy)-1 2(qyqz-qxqw)   2(qyqw+qxqz)   || mx |
			// | 2(qyqz+qxqw)   2(qxqx+qzqz)-1 2(qzqw-qxqy)   || my |
			// | 2(qyqw-qxqz)   2(qzqw+qxqy)   2(qxqx+qwqw)-1 || mz |
			bx = (qxqx+qyqy)*_2mx-mx + (qyqz-qxqw)*_2my    + (qyqw+qxqz)*_2mz;
			by = (qyqz+qxqw)*_2mx    + (qxqx+qzqz)*_2my-my + (qzqw-qxqy)*_2mz;
			bz = (qyqw-qxqz)*_2mx    + (qzqw+qxqy)*_2my    + (qxqx+qwqw)*_2mz-mz;
			bx = sqrtf(bx*bx + by*by);
			// 磁界の向きの変化量(Δm)
			dmx = (0.5f-qzqz-qwqw)*bx + (     qyqw-qxqz)*bz - mx;
			dmy = (     qyqz-qxqw)*bx + (     qzqw+qxqy)*bz - my;
			dmz = (     qyqw+qxqz)*bx + (0.5f-qyqy-qzqz)*bz - mz;
		}
		// 補正勾配＝重力の向き(g)の勾配＋方位角(b)の勾配 ⊗ 磁界の向きの変化量(Δm)
		// 勾配降下法による回転量の補正を行う
		float gradx, grady, gradz, gradw;
		// 重力の向き(g)の勾配
		gradx = 2.0f*(qy*gy -qz*gx);
		grady = 2.0f*(qx*gy +qw*gx -2.0f*qy*gz);
		gradz = 2.0f*(qw*gy -qx*gx -2.0f*qz*gz);
		gradw = 2.0f*(qz*gy +qy*gx);
		// 方位角(b)の勾配 ⊗ 磁界の向きの変化量(Δm)
		{
			float bzdmx = bz*dmx;
			float bxdmy = bx*dmy;
			float bzdmy = bz*dmy;
			float bxdmz = bx*dmz;
			gradx += qz*bxdmz                -               qz*bzdmx -qw*bxdmy+qy*bzdmy;
			grady += qw*bxdmz-2.0f*qy*bz*dmz +               qw*bzdmx +qz*bxdmy+qx*bzdmy;
			gradz += qx*bxdmz-2.0f*qz*bz*dmz -2.0f*qz*bx*dmx-qx*bzdmx +qy*bxdmy+qw*bzdmy;
			gradw += qy*bxdmz                -2.0f*qw*bx*dmx+qy*bzdmx -qx*bxdmy+qz*bzdmy;
		}
		// 補正勾配を正規化
		float k = 1.0f / sqrtf(gradx*gradx + grady*grady + gradz*gradz + gradw*gradw);
		gradx *= k, grady *= k, gradz *= k, gradw *= k;
		// 回転量に補正勾配を反映
		dqx -= beta * gradx;
		dqy -= beta * grady;
		dqz -= beta * gradz;
		dqw -= beta * gradw;
	}
	// 回転量を積算して姿勢を更新
	qx += DELTA_TIME * dqx;
	qy += DELTA_TIME * dqy;
	qz += DELTA_TIME * dqz;
	qw += DELTA_TIME * dqw;
	// 姿勢を正規化
	float k = 1.0f / sqrtf(qx*qx + qy*qy + qz*qz + qw*qw);
	qx *= k, qy *= k, qz *= k, qw *= k;
}
