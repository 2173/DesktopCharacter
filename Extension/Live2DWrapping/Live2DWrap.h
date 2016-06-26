#pragma once

namespace live2d
{
	class Live2DModelWinGL;
	class Live2DMotion;
	class MotionQueueManager;
}

class Live2DWrapping
{
public:
	static void init();

	static void destory();
};

class Live2Model
{
public:
	Live2Model();
	~Live2Model();

	/*
	* @brief 
	* @param .moc �t�@�C��
	*/
	void craeteModel( const char* str );

	/*!
	*  @brief  ���f�����폜
	*/
	void deleteModel();
	/*
	* @brief 
	* @param texture�ԍ�
	* @param gltexture�{��
	*/
	void setTexture( int textureNo, unsigned int glTexture );
	/*
	* @brief 
	* @param bool
	*/
	void setPremultipliedAlpha( bool r );
	/*
	* @brief 
	* @retval float - �L�����o�X��
	*/
	float getCanvasWidth() const;
	/*
	* @brief 
	* @retval float - �L�����o�X����
	*/
	float getCanvasHeight() const;
	/*
	* @breif 
	* @param float 16 �� �� Matrix
	*/
	void setMatrix( 
		float m00, float m01, float m02, float m03, 
		float m10, float m11, float m12, float m13, 
		float m20, float m21, float m22, float m23, 
		float m30, float m31, float m32, float m33 );
	/*
	* @brief 
	* @param int - �p�����[�^�C���f�b�N�X
	* @param float - �p�����[�^�l
	* @param float - �E�F�C�g�l( defualt = 1.0f )
	*/
	void setPrameterFloat( int index, float value, float wieght = 1.f );
	
	/*
	* @brief 
	* @param const char* - �p�����[�^��
	* @param float - �p�����[�^�l
	* @param float - �E�F�C�g�l( defualt = 1.0f )
	*/
	void setPrameterFloat( const char* name, float value, float wieght = 1.f );

	/*!
	*  @brief:	   
	*  @return:   void
	*/
	void update();
	
	/*!
	*  @brief:	  
	*  @return:   void
	*/
	void draw();

	/*!
	*  @brief:	  
	*  @return:   const live2d::Live2DModelWinGL*
	*/
	live2d::Live2DModelWinGL* getModel() const;

private:
	live2d::Live2DModelWinGL*	mLive2DModel;
};

class Live2DAnimation
{
public:
	Live2DAnimation();
	~Live2DAnimation();
	
public:
	/*!
	*  @brief:	  
	*  @param: 	  const char * filepath
	*  @return:   void
	*/
	void loadMotion( const char* filepath );

	/*!
	*  @brief  ���[�V���������
	*/
	void deleteMotion();

	/*!
	*  @brief:	  
	*  @param: 	  live2d::ALive2DModel * model
	*  @param: 	  long long timeMSec
	*  @param: 	  float weight
	*  @param: 	  MotionQueueEnt * motionQueueEnt
	*  @return:   void
	*/
	void updateParamExe( Live2Model* model, long long timeMSec, float weight );

	/*!
	*  @brief:	 
	*  @param: 	  bool loop
	*  @return:   void
	*/
	void setLoop( bool loop );

	/*!
	*  @brief:	  
	*  @return:   bool
	*/
	bool isLoop();

	/*!
	*  @brief:	  
	*  @param: 	  bool loopFadeIn
	*  @return:   void
	*/
	void setLoopFadeIn( bool loopFadeIn );

	/*!
	*  @brief:	  
	*  @return:   bool
	*/
	bool isLoopFadeIn();

	/*!
	*  @brief:	   
	*  @return:   live2d::Live2DMotion*
	*/
	live2d::Live2DMotion* getMotion() const;
	
private:
	live2d::Live2DMotion* mMotion;
};

class Live2DMotionQueueManager
{
public:
	Live2DMotionQueueManager();

	~Live2DMotionQueueManager();

public:
	/*!
	*  @brief ���[�V�����Ǘ����
	*/
	void deleteMotionManager();
	/*!
	*  @brief:	   
	*  @param: 	  AMotion * motion
	*  @param: 	  bool autoDelete
	*  @return:   int
	*/
	int startMotion( Live2DAnimation* motion, bool autoDelete );

	/*!
	*  @brief:	   
	*  @param: 	  live2d::ALive2DModel * model
	*  @return:   bool
	*/
	bool updateParam( Live2Model* model );

	/*!
	*  @brief:	   
	*  @return:   bool
	*/
	bool isFinished();
	
	/*!
	*  @brief:	   
	*  @param: 	  int motionQueueEntNo
	*  @return:   bool
	*/
	bool isFinished( int motionQueueEntNo );

	/*!
	*  @brief:	   
	*  @return:   void
	*/
	void stopAllMotions();
private:
	live2d::MotionQueueManager* mMotionManager;
};